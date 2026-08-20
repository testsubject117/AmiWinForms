# Copilot Instructions

## Project Guidelines
- User says "nighty night and 40 winks" as their sign-off phrase when ending a session.

## Session Management
- Always use the CURRENT session date for billable hours and end-of-day summary files.
- Whenever the user states the current date and time (e.g., "it's 8am on Thursday July 30, 2026"), immediately log it to the current day's billable hours file and/or kickoff/notes file as a timestamped session start reference. This ensures accurate billing and session continuity across chat summary resets.
- At the start of every session or when the user returns after signing off, automatically run Get-Date in PowerShell to confirm current date and time. If terminal is unavailable, ask the user.
- Periodically check Get-Date throughout long sessions to stay accurate.
- At end of session/sign-off, run Get-Date to confirm the closing time before logging it.
- The kickoff file for the NEXT session goes in the next day's folder. 
- Do not put today's work files in tomorrow's folder.
- Before creating ANY dated folder, file, or notes entry, always run Get-Date first to verify the correct date — never assume or guess the date from context alone.

## Notes File Management
- Notes files (EOD summaries, billable hours, kickoff notes, carry-forward files) always go in C:\Users\CapnKirk\source\Notes\ — NEVER in the repo, NOT in C:\Users\CapnKirk\Notes\. Folder format is C:\Users\CapnKirk\source\Notes\YYYY-MM-DD\ (e.g., C:\Users\CapnKirk\source\Notes\2026-08-13\). The CARRY-FORWARD-LOGIC-AUDIT.md and similar permanent files go at the root C:\Users\CapnKirk\source\Notes\ level.

## Billable Hours Management
- EOD summary files are customer-facing and must be written with full professional detail — include every task worked on, every bug fixed, every investigation performed, every decision made, and every file changed. Billable hours files can remain in short/tabular format.

## Environment Management
- When referring to the DOS environment for screenshots or testing, use Hyper-V VMs instead of DOSBox.
- Word exists and runs correctly on the DOS VM at \word2\word. Do NOT suggest "Word might be missing" as a cause for the (E) password failure — this has been confirmed and the user finds it frustrating to keep hearing it.

## Maintenance Scheduling
- Schedule updates for Windows, BitDefender, and PiKVM on the production floor's 5 machines for the next available day after 2026-08-11.
- Production backups for 2026-08-11 are already completed (done ~12:50 PM, took ~0.75 hrs).

## Build Management
- Always ask the user for approval before running a build (run_build). Do not build automatically after making code changes.