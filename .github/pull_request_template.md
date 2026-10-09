## What does this change?

Briefly describe the change and which section/project it touches.

## Why?

Link the issue or explain the motivation.

## Safety checklist

- [ ] No deletion happens without an explicit confirmation.
- [ ] Risky operations create a backup / restore point first.
- [ ] New user-facing strings added to BOTH `strings.en.json` and `strings.ar.json`.
- [ ] All system logic lives in `VoidRemove.Core`; UIs only bind to it.
- [ ] `python tools/verify_repo.py` passes locally.

## Testing

How was this verified (tests added / manual on Windows)?
