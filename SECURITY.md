# Security Policy

VoidRemove runs with elevated privileges and modifies real systems, so security reports are
treated with priority.

## Supported versions

Only the latest release on the [releases page](https://github.com/yswef/VoidRemove/releases)
is supported. Please update before reporting.

## Reporting a vulnerability

- Do **not** open a public issue for a security vulnerability.
- Report privately via the repository's **Security → Advisories** tab, or by contacting the
  maintainers directly.
- Include: a description, affected component, reproduction steps, and impact.

We acknowledge receipt within 3 days and aim to give a remediation timeline within 7.

## Scope

In scope: privilege escalation, arbitrary file deletion outside the safety model, secret
leakage of the optional VirusTotal key, and unsafe default behaviour that bypasses
confirmation/backup rules.

Out of scope: issues requiring physical access, or behaviour that is by design (for example,
an administrator choosing the Advanced safety level).

## Safe-handling expectations

Any fix that changes deletion/quarantine behaviour must preserve: no deletion without
confirmation, backups for risky operations, and the permanent exclusion list.
