---
name: crystallized-b95cdfe6a2b4bea6
description: Crystallized repair pattern (3 successes) for stack fastcrmcrm|python|fastapi
version: 1.0.0
crystallized: true
error-signature: d6efe80788b07c58634655905239ffe726ad13458d065820e88238f134c9936b
approval: active
allowed-tools: [apply_patch, edit_file, write_file, bash, run_build, run_tests]
---

# Crystallized Repair Skill

## Trigger Conditions
- Stack pattern: `fastcrmcrm|python|fastapi`
- Error signature: `d6efe80788b07c58634655905239ffe726ad13458d065820e88238f134c9936b`
- Playbook score: 0,67 (success=3, fail=3)

## Fix Steps
1. Reproduce using the error signature and recent build log.
2. Apply fix pattern: `repair_session:2_patches`
3. Verify with `run_build` then `run_tests` if applicable.
4. Keep the diff minimal — patch only files implicated by the error.

## Example Diff
```diff
# derived from successful pattern: repair_session:2_patches
# inspect rollout tool outputs for concrete patch hunks
```
