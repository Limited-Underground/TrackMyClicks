workspace "TrackMyClicks by Limited Underground" "Frozen aggregate-only Windows counter. Public 60e0a75; unpublished P0 differences are labeled, not integrated." {
    !impliedRelationships false
    model {
        owner = person "Owner" "Uses local activity summaries and explicit pause/tray controls."
        windows = softwareSystem "Windows 11 interactive desktop" "Native input hook chain, notification area, WPF/Windows runtime and local OS SQLite library; not a network service." {
            tags "External"
        }
        tracker = softwareSystem "TrackMyClicks" "Local anonymous aggregate activity counter. Product development frozen; native usability and foreground CPU acceptance open." {
            desktop = container "Windows desktop application" "One process: WPF UI/lifetime, WinForms tray, InputHooks, buffering and Core aggregate/insight logic. Baseline opens dashboard; preserved P0 overlay starts hidden." "C# / .NET 10 / WPF"
            profile = container "Local activity profile" "Daily/lifetime totals, settings, earned badges and Version 1 insight/challenge state; no input content. Owner profiles excluded from foundation inspection." "SQLite via winsqlite3.dll / WAL" {
                tags "Database"
            }
            desktop -> profile "Reads and transactionally saves anonymous aggregates; coherent backup before schema-0 migration" "Local SQLite API"
            owner -> desktop "Views summaries, pauses/resumes, explicitly opens or exits" "Local dashboard / tray"
            windows -> desktop "Supplies mouse and keyboard observations; key identities transient for repeat suppression" "WH_MOUSE_LL / WH_KEYBOARD_LL"
            desktop -> windows "Forwards input to the next hook; presents UI/tray and optional notices" "Win32 / WPF / NotifyIcon"
        }
        owner -> tracker "Evaluates local activity and controls tracking"
        windows -> tracker "Delivers system-wide interactive input signals"
        tracker -> windows "Forwards input and uses local desktop facilities; no runtime network"
    }
    views {
        systemContext tracker "TrackMyClicksContext" "Owner and Windows boundaries; no cloud, accounts, telemetry or background service." {
            include *
            autoLayout lr
        }
        container tracker "TrackMyClicksContainers" "One desktop runtime and one local datastore; Core is a library, not a separate deployed service." {
            include *
            autoLayout lr
        }
        styles {
            element "Element" {
                color #ffffff
            }
            element "Person" {
                shape Person
                background #174a72
            }
            element "Software System" {
                background #174a72
            }
            element "Container" {
                background #276a9b
            }
            element "External" {
                background #616b75
            }
            element "Database" {
                shape Cylinder
                background #366943
            }
        }
    }
}
