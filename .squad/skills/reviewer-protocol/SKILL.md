---
name: reviewer-protocol
description: Strict review lockout and deadlock escalation protocol
domain: code-review
confidence: high
source: inherited from Skyflow project
---

## Protocol

### Review Assignment
1. The author requests review from the most relevant domain owner
2. The reviewer must respond within the current session
3. Reviews are either APPROVE or REJECT — no "looks good with changes"

### Rejection Lockout
When a reviewer rejects:
1. The author must address ALL feedback
2. The same reviewer must re-review
3. No other agent can override the rejection
4. The author cannot self-approve after rejection

### Deadlock Escalation
If author and reviewer disagree after 2 rounds:
1. Escalate to the Lead (Holden)
2. Lead makes the final decision
3. Decision is recorded in `.squad/decisions.md`
4. Both parties accept the Lead's decision

## Anti-Patterns
- ❌ Merging after rejection without re-review
- ❌ Rubber-stamp approvals without reading the code
- ❌ Reviewer blocking without actionable feedback
- ❌ Escalating before 2 rounds of discussion
