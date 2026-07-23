# Test Failure Classification Governance

No existing repository-wide session-conventions or test-failure-classification document was found when
this standing rule was recorded. This focused document therefore owns the rule without attaching it to an
unrelated phase report.

Classifications such as "unrelated" or "pre-existing" applied to test failures must be backed by at least
one of: a prior clean baseline run, a known-flaky-test list, or an isolated revert-and-rerun. A bare assertion
of "unrelated" without one of these is not sufficient and must not be treated as final in any future phase
report.
