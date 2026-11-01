// Copyright 2026 Nenad Bjelanovic
// SPDX-License-Identifier: Apache-2.0
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
namespace TrackMyClicks.Core;

public sealed class Store : IDisposable
{
    private IntPtr db;
    public int SchemaVersion { get; private set; }
    public string? MigrationBackupPath { get; private set; }
    public Store(string path)
    {
        path=Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (Native.sqlite3_open(Utf8(path), out db) != 0) { Dispose(); throw new IOException("Cannot open local activity database."); }
        try
        {
            Execute("PRAGMA busy_timeout=3000;");
            SchemaVersion=int.Parse(Query("PRAGMA user_version;")[0][0],System.Globalization.CultureInfo.InvariantCulture);
            if(SchemaVersion>1) throw new IOException($"Activity database schema {SchemaVersion} is newer than this application supports. No migration was performed.");
            if(SchemaVersion==0)
            {
                bool existing=Query("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';").Count>0;
                if(existing) MigrationBackupPath=BackupBeforeMigration(path);
                try
                {
                    Execute("BEGIN IMMEDIATE; CREATE TABLE IF NOT EXISTS totals(day TEXT PRIMARY KEY,l INTEGER NOT NULL,r INTEGER NOT NULL,m INTEGER NOT NULL,k INTEGER NOT NULL,d REAL NOT NULL,s REAL NOT NULL); CREATE TABLE IF NOT EXISTS achievements(id TEXT PRIMARY KEY); CREATE TABLE IF NOT EXISTS settings(name TEXT PRIMARY KEY,value TEXT NOT NULL); CREATE TABLE expansion_state(id INTEGER PRIMARY KEY CHECK(id=1),json TEXT NOT NULL); PRAGMA user_version=1; COMMIT;");
                }
                catch { try { Execute("ROLLBACK;"); } catch { } throw; }
                SchemaVersion=1;
            }
            Execute("PRAGMA journal_mode=WAL; PRAGMA synchronous=FULL;");
        }
        catch { Dispose(); throw; }
    }
    private string BackupBeforeMigration(string sourcePath)
    {
        string path=sourcePath+".pre-v03-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ",System.Globalization.CultureInfo.InvariantCulture)+"-"+Guid.NewGuid().ToString("N")+".bak";
        // Reserve with CreateNew: never replace a previous preservation checkpoint.
        using(var reservation=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)) { }
        IntPtr target=IntPtr.Zero,backup=IntPtr.Zero;
        try
        {
            if(Native.sqlite3_open(Utf8(path),out target)!=0) throw new IOException("Cannot open pre-migration SQLite backup.");
            backup=Native.sqlite3_backup_init(target,Utf8("main"),db,Utf8("main"));
            if(backup==IntPtr.Zero) throw new IOException("Cannot initialize pre-migration SQLite backup.");
            int code; int attempts=0;
            while((code=Native.sqlite3_backup_step(backup,-1)) is 5 or 6 && attempts++<40) Thread.Sleep(25);
            if(code!=101) throw new IOException($"Pre-migration SQLite backup failed ({code}); source migration was not attempted.");
            int finished=Native.sqlite3_backup_finish(backup); backup=IntPtr.Zero;
            if(finished!=0) throw new IOException($"Pre-migration SQLite backup could not finish ({finished}); source migration was not attempted.");
            return path;
        }
        finally
        {
            if(backup!=IntPtr.Zero) Native.sqlite3_backup_finish(backup);
            if(target!=IntPtr.Zero) Native.sqlite3_close(target);
        }
    }
    private static byte[] Utf8(string value) => Encoding.UTF8.GetBytes(value + '\0');
    private void Execute(string sql)
    {
        int code = Native.sqlite3_exec(db,Utf8(sql),IntPtr.Zero,IntPtr.Zero,out IntPtr error);
        if (code == 0) return;
        string message = Marshal.PtrToStringUTF8(error) ?? "SQLite error"; Native.sqlite3_free(error); throw new IOException(message);
    }
    private List<string[]> Query(string sql)
    {
        var rows = new List<string[]>();
        Native.Callback callback = (_, count, values, _) => { var row = new string[count]; for (int i=0;i<count;i++) row[i] = Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(values,i*IntPtr.Size)) ?? ""; rows.Add(row); return 0; };
        int code = Native.sqlite3_exec(db,Utf8(sql),callback,IntPtr.Zero,out IntPtr error);
        GC.KeepAlive(callback);
        if (code != 0) { string message = Marshal.PtrToStringUTF8(error) ?? "SQLite query error"; Native.sqlite3_free(error); throw new IOException(message); }
        return rows;
    }
    private static string Q(string value) => "'" + value.Replace("'","''") + "'";
    public Totals ReadTotals(string day)
    {
        var rows = Query("SELECT l,r,m,k,d,s FROM totals WHERE day="+Q(day));
        if(rows.Count == 0) return new(); var r=rows[0]; var culture=System.Globalization.CultureInfo.InvariantCulture;
        return new() { LeftClicks=long.Parse(r[0],culture),RightClicks=long.Parse(r[1],culture),MiddleClicks=long.Parse(r[2],culture),KeyPresses=long.Parse(r[3],culture),DistancePixels=double.Parse(r[4],culture),ScrollUnits=double.Parse(r[5],culture) };
    }
    public HashSet<string> ReadAchievements() => Query("SELECT id FROM achievements").Select(r=>r[0]).ToHashSet();
    public string? ReadSetting(string name) => Query("SELECT value FROM settings WHERE name="+Q(name)).FirstOrDefault()?[0];
    public string? ReadExpansionState() => Query("SELECT json FROM expansion_state WHERE id=1;").FirstOrDefault()?[0];
    public IReadOnlyDictionary<string,Totals> ReadDailyTotals()
    {
        var days=Query("SELECT day FROM totals WHERE day!='lifetime' ORDER BY day;");
        return days.ToDictionary(r=>r[0],r=>ReadTotals(r[0]),StringComparer.Ordinal);
    }
    public void Save(string day, Totals today, Totals lifetime, IEnumerable<string> achievements, bool paused,bool notifications,bool metric,double pixelsPerInch=96,string? expansionState=null)
    {
        if(expansionState!=null)
        {
            using var parsed=JsonDocument.Parse(expansionState);
            if(parsed.RootElement.ValueKind!=JsonValueKind.Object) throw new ArgumentException("Expansion state must be a JSON object.",nameof(expansionState));
        }
        string Row(string key,Totals t) => FormattableString.Invariant($"INSERT OR REPLACE INTO totals VALUES({Q(key)},{t.LeftClicks},{t.RightClicks},{t.MiddleClicks},{t.KeyPresses},{t.DistancePixels:R},{t.ScrollUnits:R});");
        var sql=new StringBuilder("BEGIN IMMEDIATE;").Append(Row(day,today)).Append(Row("lifetime",lifetime));
        foreach(var id in achievements) sql.Append("INSERT OR IGNORE INTO achievements VALUES(").Append(Q(id)).Append(");");
        foreach(var setting in new[]{("paused",paused),("notifications",notifications),("metric",metric)}) sql.Append("INSERT OR REPLACE INTO settings VALUES(").Append(Q(setting.Item1)).Append(',').Append(Q(setting.Item2 ? "1":"0")).Append(");");
        sql.Append("INSERT OR REPLACE INTO settings VALUES('ppi',").Append(Q(pixelsPerInch.ToString("R",System.Globalization.CultureInfo.InvariantCulture))).Append(");");
        if(expansionState!=null) sql.Append("INSERT OR REPLACE INTO expansion_state VALUES(1,").Append(Q(expansionState)).Append(");");
        sql.Append("COMMIT;");
        try { Execute(sql.ToString()); } catch { try { Execute("ROLLBACK;"); } catch { } throw; }
    }
    public void Dispose() { if(db != IntPtr.Zero) { Native.sqlite3_close(db); db=IntPtr.Zero; } }
    private static class Native
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate int Callback(IntPtr context,int count,IntPtr values,IntPtr names);
        [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] public static extern int sqlite3_open(byte[] filename,out IntPtr database);
        [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] public static extern int sqlite3_close(IntPtr database);
        [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] public static extern int sqlite3_exec(IntPtr database,byte[] sql,IntPtr callback,IntPtr context,out IntPtr error);
        [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] public static extern int sqlite3_exec(IntPtr database,byte[] sql,Callback callback,IntPtr context,out IntPtr error);
        [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] public static extern void sqlite3_free(IntPtr value);
        [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] public static extern IntPtr sqlite3_backup_init(IntPtr destination,byte[] destinationName,IntPtr source,byte[] sourceName);
        [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] public static extern int sqlite3_backup_step(IntPtr backup,int pages);
        [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] public static extern int sqlite3_backup_finish(IntPtr backup);
    }
}

