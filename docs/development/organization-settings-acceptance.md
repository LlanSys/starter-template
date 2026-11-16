# Organization settings - Allan Run supplement

Use this alongside your independent operator pack. These are fresh manual checks, not pre-filled pass results. Reuse generated QA identities through scripts/Get-LocalQaLogin.ps1; do not place passwords, seeds or codes in results files.

| ID | Action | Expected | Your result |
| --- | --- | --- | --- |
| ORG-01 | Open each customer's own /iam/sign-in?workspace=<identifier> link | Correct organization name; credential/provider controls ready together | Not run |
| ORG-02 | Type a draft username, use Change Organization, try an unavailable identifier, then select the other customer | Draft cleared; unavailable selection cannot fall back to prior credentials; correct name and URL after selection | Not run |
| ORG-03 | Sign in as each tenant administrator with MFA; open Admin Center > Org Settings | Five catalogue rows, defaults, previews, stable action positions and history | Not run |
| ORG-04 | Set tenant A to Africa/Nairobi; leave B on UTC | A timestamp displays +03:00, B stays +00:00; underlying instant is unchanged | Not run |
| ORG-05 | Set Regional Format to French (France), Date Format to ISO, Time Format to 12-Hour | Preview uses regional number separators, ISO date and AM/PM; Profile timestamps use the same format and zone label | Not run |
| ORG-06 | Change Default Rows Per Page to 10; navigate to a supported listing | Initial page size is 10, with API bounds preserved | Not run |
| ORG-07 | Open the same setting in two independent administrator sessions; save one, then save the stale form | First save succeeds; stale save asks for refresh, without overwriting it | Not run |
| ORG-08 | Use More > Reset To Default for every temporary override | Source changes to Default; current vendor default is restored; previous/new values and reset remain in history | Not run |
| ORG-09 | Sign in as coordinator/reader without settings-update permission | Allowed display formatting works; administration changes remain unavailable/denied | Not run |
| ORG-10 | Reload and check Profile and the workspace header | Correct organization remains selected; no generic product-name substitution for the selected customer | Not run |

After the run, reset temporary overrides or record the values you intentionally keep. Time zones do not alter TimeProvider, stored UTC timestamps, MFA/session/invitation expiry or KYC retention periods. Use your original pack for the deferred fresh Google/Microsoft sign-in plus app MFA checks; connecting a provider alone is not that test.
