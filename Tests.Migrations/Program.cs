// Copyright 2026 Nenad Bjelanovic
// SPDX-License-Identifier: Apache-2.0
using System.Runtime.InteropServices;
using System.Text;
using TrackMyClicks.Core;

string root=Path.Combine(AppContext.BaseDirectory,"migration-data",Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root); int checks=0;
void Assert(bool value,string name) { if(!value) throw new Exception("FAIL: "+name); checks++; Console.WriteLine("PASS: "+name); }
string path=Path.Combine(root,"v02.sqlite3");
using(var legacy=new RawDb(path))
{
    legacy.Exec("PRAGMA journal_mode=WAL; CREATE TABLE totals(day TEXT PRIMARY KEY,l INTEGER NOT NULL,r INTEGER NOT NULL,m INTEGER NOT NULL,k INTEGER NOT NULL,d REAL NOT NULL,s REAL NOT NULL); CREATE TABLE achievements(id TEXT PRIMARY KEY); CREATE TABLE settings(name TEXT PRIMARY KEY,value TEXT NOT NULL); INSERT INTO totals VALUES('2026-10-27',5,6,7,123,456.5,12.5),('2026-10-28',1,2,3,10,100,5),('lifetime',6,8,10,133,556.5,17.5); INSERT INTO achievements VALUES('click100'),('key1000'); INSERT INTO settings VALUES('paused','1'),('notifications','0'),('metric','1'),('ppi','192');");
    string backup;
    using(var upgraded=new Store(path))
    {
        Assert(upgraded.SchemaVersion==1,"V0.2 schema upgrades forward to version1");
        var historical=upgraded.ReadTotals("2026-10-27");
        Assert(historical.LeftClicks==5 && historical.RightClicks==6 && historical.MiddleClicks==7 && historical.KeyPresses==123 && historical.DistancePixels==456.5 && historical.ScrollUnits==12.5,"upgrade retains every daily aggregate");
        var lifetime=upgraded.ReadTotals("lifetime");
        Assert(lifetime.Clicks==24 && lifetime.KeyPresses==133 && lifetime.DistancePixels==556.5 && lifetime.ScrollUnits==17.5,"upgrade retains lifetime values");
        Assert(upgraded.ReadAchievements().SetEquals(new[]{"click100","key1000"}),"upgrade preserves existing unlocked achievement IDs");
        Assert(upgraded.ReadSetting("paused")=="1" && upgraded.ReadSetting("notifications")=="0" && upgraded.ReadSetting("metric")=="1" && upgraded.ReadSetting("ppi")=="192","upgrade preserves all existing settings");
        Assert(upgraded.ReadExpansionState()==null,"legacy database initially has no fabricated expansion history");
        Assert(upgraded.ReadDailyTotals().Count==2 && !upgraded.ReadDailyTotals().ContainsKey("lifetime"),"daily history API excludes lifetime sentinel");
        backup=upgraded.MigrationBackupPath ?? throw new Exception("Expected backup path");
        using(var saved=new RawDb(backup))
        {
            Assert(saved.Query("PRAGMA user_version;")=="0" && saved.Query("SELECT sum(l+r+m) FROM totals WHERE day='lifetime';")=="24","online backup retains pre-migration version and WAL-committed totals");
            Assert(saved.Query("SELECT count(*) FROM achievements;")=="2" && saved.Query("PRAGMA integrity_check;")=="ok","pre-migration backup has achievements and valid SQLite integrity");
        }
        upgraded.Save("2026-10-28",new(){LeftClicks=2},lifetime,new[]{"click100","key1000"},false,true,false,110,"{\"rewards\":[\"2026-10-28:clicks\"],\"quote\":\"it's local\"}");
        Assert(upgraded.ReadExpansionState()!.Contains("it's local"),"expansion state JSON stores quotes safely with aggregates");
        using(var current=new RawDb(path)) current.Exec("CREATE TRIGGER fail_expansion BEFORE INSERT ON expansion_state BEGIN SELECT RAISE(ABORT,'fixture failure'); END;");
        bool failed=false;
        try { upgraded.Save("2026-10-28",new(){LeftClicks=999},new(){LeftClicks=999},new[]{"new-unlock"},true,false,true,96,"{\"shouldRollback\":true}"); } catch(IOException) { failed=true; }
        Assert(failed && upgraded.ReadTotals("2026-10-28").LeftClicks==2 && upgraded.ReadTotals("lifetime").Clicks==24 && !upgraded.ReadAchievements().Contains("new-unlock") && upgraded.ReadSetting("paused")=="0","expansion save failure rolls back aggregate, unlock and settings writes atomically");
        Assert(upgraded.ReadExpansionState()!.Contains("rewards"),"failed save retains previous expansion state");
        using(var current=new RawDb(path)) current.Exec("DROP TRIGGER fail_expansion;");
        bool invalid=false; try { upgraded.Save("2026-10-28",new(){LeftClicks=888},lifetime,[],false,true,false,96,"not JSON"); } catch(System.Text.Json.JsonException) { invalid=true; }
        Assert(invalid && upgraded.ReadTotals("2026-10-28").LeftClicks==2,"invalid JSON rejected before any database write");
    }
    using(var reopened=new Store(path))
    {
        Assert(reopened.SchemaVersion==1 && reopened.MigrationBackupPath==null && Directory.GetFiles(root,"*.bak").Length==1,"restart does not repeat migration or backup");
        Assert(reopened.ReadExpansionState()!.Contains("rewards") && reopened.ReadTotals("2026-10-28").LeftClicks==2,"expansion state and activity persist across restart");
        reopened.Save("2026-10-28",new(){LeftClicks=3},reopened.ReadTotals("lifetime"),reopened.ReadAchievements(),false,true,false);
        Assert(reopened.ReadExpansionState()!.Contains("rewards"),"legacy Save overload retains expansion state");
    }
}
string fresh=Path.Combine(root,"fresh.sqlite3");
using(var created=new Store(fresh)) Assert(created.SchemaVersion==1 && created.MigrationBackupPath==null,"new empty database initializes latest schema without unnecessary backup");
string future=Path.Combine(root,"future.sqlite3");
using(var raw=new RawDb(future)) raw.Exec("CREATE TABLE future_only(value TEXT); INSERT INTO future_only VALUES('preserve me'); PRAGMA user_version=2;");
bool rejected=false; try { using var unsupported=new Store(future); } catch(IOException) { rejected=true; }
using(var raw=new RawDb(future)) Assert(rejected && raw.Query("PRAGMA user_version;")=="2" && raw.Query("SELECT value FROM future_only;")=="preserve me" && raw.Query("SELECT count(*) FROM sqlite_master WHERE name='expansion_state';")=="0","future schema is rejected without downgrade or data mutation");
string broken=Path.Combine(root,"migration-conflict.sqlite3");
using(var raw=new RawDb(broken)) raw.Exec("CREATE TABLE expansion_state(legacy TEXT); INSERT INTO expansion_state VALUES('do not replace'); PRAGMA user_version=0;");
bool conflict=false; try { using var unsupported=new Store(broken); } catch(IOException) { conflict=true; }
using(var raw=new RawDb(broken)) Assert(conflict && raw.Query("PRAGMA user_version;")=="0" && raw.Query("SELECT legacy FROM expansion_state;")=="do not replace" && raw.Query("SELECT count(*) FROM sqlite_master WHERE name='totals';")=="0","migration conflict rolls back forward-version change and all schema changes");
Console.WriteLine($"{checks} migration/persistence checks passed. Evidence: {root}");

sealed class RawDb : IDisposable
{
    private IntPtr db;
    private static byte[] U(string s)=>Encoding.UTF8.GetBytes(s+'\0');
    public RawDb(string path) { if(Open(U(path),out db)!=0) throw new IOException("Fixture open failed"); }
    public void Exec(string sql) { int code=Execute(db,U(sql),IntPtr.Zero,IntPtr.Zero,out var err); if(code!=0){string msg=Marshal.PtrToStringUTF8(err)??"Fixture error"; Free(err);throw new IOException(msg);} }
    public string? Query(string sql)
    {
        string? value=null; Callback cb=(_,count,values,_)=>{if(count>0)value=Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(values));return 0;};
        int code=QueryNative(db,U(sql),cb,IntPtr.Zero,out var err);GC.KeepAlive(cb);if(code!=0){string msg=Marshal.PtrToStringUTF8(err)??"Fixture query error";Free(err);throw new IOException(msg);} return value;
    }
    public void Dispose(){if(db!=IntPtr.Zero){Close(db);db=IntPtr.Zero;}}
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int Callback(IntPtr ctx,int count,IntPtr values,IntPtr names);
    [DllImport("winsqlite3.dll",EntryPoint="sqlite3_open",CallingConvention=CallingConvention.Cdecl)] private static extern int Open(byte[] path,out IntPtr db);
    [DllImport("winsqlite3.dll",EntryPoint="sqlite3_close",CallingConvention=CallingConvention.Cdecl)] private static extern int Close(IntPtr db);
    [DllImport("winsqlite3.dll",EntryPoint="sqlite3_exec",CallingConvention=CallingConvention.Cdecl)] private static extern int Execute(IntPtr db,byte[] sql,IntPtr cb,IntPtr ctx,out IntPtr err);
    [DllImport("winsqlite3.dll",EntryPoint="sqlite3_exec",CallingConvention=CallingConvention.Cdecl)] private static extern int QueryNative(IntPtr db,byte[] sql,Callback cb,IntPtr ctx,out IntPtr err);
    [DllImport("winsqlite3.dll",EntryPoint="sqlite3_free",CallingConvention=CallingConvention.Cdecl)] private static extern void Free(IntPtr p);
}
