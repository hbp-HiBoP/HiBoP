# Setup handoff

Project-local relay proposal for the approved HiBoP Quest synchronization plan. T00 is retained as precompleted solely on the user's assertion; Relay must not execute it. T01–T18 retain their original order and dependencies. Implementation tasks remain unready until their task-specific Unity filters, write scopes, and required measurements or product decisions are resolved.

## Tasks completed before Relay
- T00: The user confirms that T00 was implemented and tested before Relay. Relay has not rerun those checks and must not execute T00.
These tasks are trusted as part of the user-approved starting baseline; Relay does not claim to have re-run their acceptance checks.

## Explicitly mutable plan outputs

- ../operation-matrix.md
These are implementation/evidence outputs, not editable acceptance criteria. Inspect this distinction before approval.

## Unresolved items

- Pilot the selected models under the user's account; setup cannot establish model access.
- Confirm that the documented Unity MCP endpoint is reachable, identify the live HiBoP instance, and verify run_tests/get_test_job, project state, and console access before any Unity task starts.
- Define focused future test filters, expected full test names, minimum counts, and compilation targets as each implementation contract is refined; existing tier categories alone cannot prove newly required behavior.
- Resolve later task write scopes against the actual files and prefab references before starting them. Do not grant broad or other-repository scope by implication.
- Retain unavailable physical and p95 measurements as manual/unresolved evidence until the plan's sample counts and device conditions are met.

## Approval required

Inspect the generated tasks, scopes, real verification commands and source coverage. Then use setup-approve with an explicit note. Setup does not run tests or implementation.
