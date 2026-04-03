---
description: "Analyze crossgen2 outerloop pipeline failures and compare with the runtime pipeline to identify unique failures that need new GitHub issues"

permissions:
  contents: read
  issues: read
  pull-requests: read

network:
  allowed:
    - defaults
    - https://dev.azure.com
    - https://helix.dot.net

tools:
  github:
    mode: remote
    toolsets: [default, search]
  web-fetch:

safe-outputs:
  create-issue:
    max: 10
    labels:
      - "Known Build Error"
      - "area-crossgen2-coreclr"
  noop:
    report-as-issue: false

on:
  schedule:
    - cron: "0 9 * * *"  # Daily at 9 AM UTC, after the crossgen2 outerloop pipeline runs at 5 AM UTC
  workflow_dispatch:

# ###############################################################
# Override the COPILOT_GITHUB_TOKEN secret usage for the workflow
# with a randomly-selected token from a pool of secrets.
#
# As soon as organization-level billing is offered for Agentic
# Workflows, this stop-gap approach will be removed.
#
# See: /.github/actions/select-copilot-pat/README.md
# ###############################################################

  # Add the pre-activation step of selecting a random PAT from the supplied secrets
  steps:
    - uses: actions/checkout@de0fac2e4500dabe0009e67214ff5f5447ce83dd # v6.0.2
      name: Checkout the select-copilot-pat action folder
      with:
        persist-credentials: false
        sparse-checkout: .github/actions/select-copilot-pat
        sparse-checkout-cone-mode: true
        fetch-depth: 1

    - id: select-copilot-pat
      name: Select Copilot token from pool
      uses: ./.github/actions/select-copilot-pat
      env:
        SECRET_0: ${{ secrets.COPILOT_PAT_0 }}
        SECRET_1: ${{ secrets.COPILOT_PAT_1 }}
        SECRET_2: ${{ secrets.COPILOT_PAT_2 }}
        SECRET_3: ${{ secrets.COPILOT_PAT_3 }}
        SECRET_4: ${{ secrets.COPILOT_PAT_4 }}
        SECRET_5: ${{ secrets.COPILOT_PAT_5 }}
        SECRET_6: ${{ secrets.COPILOT_PAT_6 }}
        SECRET_7: ${{ secrets.COPILOT_PAT_7 }}
        SECRET_8: ${{ secrets.COPILOT_PAT_8 }}
        SECRET_9: ${{ secrets.COPILOT_PAT_9 }}

# Add the pre-activation output of the randomly selected PAT
jobs:
  pre-activation:
    outputs:
      copilot_pat_number: ${{ steps.select-copilot-pat.outputs.copilot_pat_number }}

# Override the COPILOT_GITHUB_TOKEN expression used in the activation job
# Consume the PAT number from the pre-activation step and select the corresponding secret
engine:
  id: copilot
  model: claude-opus-4.6
  env:
    # We cannot use line breaks in this expression as it leads to a syntax error in the compiled workflow
    # If none of the `COPILOT_PAT_#` secrets were selected, then the default COPILOT_GITHUB_TOKEN is used
    COPILOT_GITHUB_TOKEN: ${{ case(needs.pre_activation.outputs.copilot_pat_number == '0', secrets.COPILOT_PAT_0, needs.pre_activation.outputs.copilot_pat_number == '1', secrets.COPILOT_PAT_1, needs.pre_activation.outputs.copilot_pat_number == '2', secrets.COPILOT_PAT_2, needs.pre_activation.outputs.copilot_pat_number == '3', secrets.COPILOT_PAT_3, needs.pre_activation.outputs.copilot_pat_number == '4', secrets.COPILOT_PAT_4, needs.pre_activation.outputs.copilot_pat_number == '5', secrets.COPILOT_PAT_5, needs.pre_activation.outputs.copilot_pat_number == '6', secrets.COPILOT_PAT_6, needs.pre_activation.outputs.copilot_pat_number == '7', secrets.COPILOT_PAT_7, needs.pre_activation.outputs.copilot_pat_number == '8', secrets.COPILOT_PAT_8, needs.pre_activation.outputs.copilot_pat_number == '9', secrets.COPILOT_PAT_9, secrets.COPILOT_GITHUB_TOKEN) }}
---

# Crossgen2 Outerloop Pipeline Failure Analysis

You are a CI pipeline analyzer for the `dotnet/runtime` repository. Your task is to analyze failures from the most recent crossgen2 outerloop pipeline run on `main`, compare them against the last `runtime` pipeline run on `main` that started before it, and create GitHub issues only for failures that are **unique to the crossgen2 outerloop** pipeline.

The key insight: if a test or job already failed in the regular `runtime` pipeline, that failure is already tracked and does not need a separate crossgen outerloop issue.

## Step 1: Find the Most Recent Crossgen2 Outerloop Pipeline Run

Use `web-fetch` to query the Azure DevOps REST API. Try both the public and internal projects:

1. Find the crossgen2 outerloop pipeline definition. Try the public project first:
   ```
   GET https://dev.azure.com/dnceng-public/public/_apis/build/definitions?name=crossgen2-outerloop&api-version=7.0
   ```
   If the definition is not found there, try the internal project:
   ```
   GET https://dev.azure.com/dnceng/internal/_apis/build/definitions?name=crossgen2-outerloop&api-version=7.0
   ```

2. Using the definition ID found above, get the most recent completed run on `main`:
   ```
   GET https://dev.azure.com/{org}/{project}/_apis/build/builds?definitions={defId}&branchName=refs/heads/main&statusFilter=completed&$top=1&api-version=7.0
   ```

Record the crossgen2 outerloop build's **build ID**, **start time**, **result** (`succeeded`, `failed`, or `partiallySucceeded`), and **build URL**.

If no completed run is found or the run **succeeded**, use the `noop` safe output to report that no action is needed, then stop.

## Step 2: Get the Failed Jobs from the Crossgen2 Outerloop Run

Fetch the build timeline to get all jobs and their results:
```
GET https://dev.azure.com/{org}/{project}/_apis/build/builds/{crossgenBuildId}/timeline?api-version=7.0
```

From the timeline, collect every record where `type` is `"Job"` and `result` is `"failed"`. For each failed job, record:
- The **job name** (e.g., `Run_Tests linux_x64_checked_outerloop_ReadyToRun`)
- The **stage name** (parent record with `type == "Stage"`)
- Any available **error details**

## Step 3: Find the Last Runtime Pipeline Run Before the Crossgen Outerloop Run

Find the `runtime` pipeline definition in the same AzDO organization and project as the crossgen2 outerloop pipeline:
```
GET https://dev.azure.com/{org}/{project}/_apis/build/definitions?name=runtime&api-version=7.0
```

Then get recent completed runs on `main`:
```
GET https://dev.azure.com/{org}/{project}/_apis/build/builds?definitions={runtimeDefId}&branchName=refs/heads/main&statusFilter=completed&$top=10&api-version=7.0
```

From the results, select the most recent build whose `startTime` is **earlier than** the crossgen2 outerloop build's `startTime`. This is the baseline runtime run to compare against.

If no such runtime run is found, use the `noop` safe output to report this situation, then stop.

## Step 4: Get the Failed Jobs from the Runtime Run

Fetch the build timeline for the runtime build, using the same API as Step 2:
```
GET https://dev.azure.com/{org}/{project}/_apis/build/builds/{runtimeBuildId}/timeline?api-version=7.0
```

Collect the same information: failed jobs with their names and stages.

## Step 5: Identify Failures Unique to the Crossgen2 Outerloop

For each failed job from the crossgen2 outerloop run, check whether the **same job name** (or a very similar one) also failed in the runtime run. Two jobs match if their names are the same or differ only in configuration suffixes that appear in both pipelines (e.g., platform and arch identifiers like `linux_x64`).

Classify each crossgen2 outerloop failure as one of:
- **Shared**: The same job also failed in the runtime run → already tracked, skip.
- **Unique**: The job did NOT fail in the runtime run → needs investigation.

## Step 6: Check for Existing "Known Build Error" Issues

For each **unique** crossgen2 outerloop failure, search GitHub to avoid filing a duplicate:
```
repo:dotnet/runtime is:issue is:open label:"Known Build Error" {job-name-keywords}
```

If a matching open issue already exists, skip that failure (no new issue needed).

## Step 7: Create Issues for New Unique Failures

For each unique crossgen2 outerloop failure that has **no** existing tracking issue, use the `create-issue` safe output to file a new GitHub issue.

The issue body must include:
- A brief description of what failed and on which platform/configuration.
- A link to the failing crossgen2 outerloop AzDO build (the URL from Step 1).
- The job name and stage where the failure occurred.
- Any available error snippet from the build timeline.
- A note that this failure was **not** present in the runtime pipeline run, with a link to that runtime build (from Step 3) for comparison.

## Step 8: Post a Summary

After processing all failures, use the `noop` safe output to post a Markdown summary to the workflow step summary. The summary must include:

| Section | Content |
|---------|---------|
| Crossgen2 outerloop build | Build ID, result, and URL |
| Runtime comparison build | Build ID, start time, and URL |
| Shared failures | List of failures skipped because they also appeared in the runtime run |
| Existing issues | List of failures skipped because a tracking issue already exists |
| New issues created | List of issues created (title + link), or "none" if everything was already tracked |
