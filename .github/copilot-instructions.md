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

## Environment Management
- When referring to the DOS environment for screenshots or testing, use Hyper-V VMs instead of DOSBox.