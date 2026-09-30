# 21. Lifecycle characterization and a final demonstration

The [lifecycle generator](lab/generate_lifecycle_lab.py) copies seven current domain source files into a new external directory. It compiles them with a small [test program](lab/LifecycleProgram.cs.txt). This is a characterization lab: several passing observations describe behavior that a capstone might deliberately improve. A green result does not endorse every observed behavior as the desired product contract.

The prerequisite is chapters 08 through 20. Before running anything, predict all six observations. Restoration selects an existing version without appending history. Adding a version retains earlier text while setting PendingScan. The current folder mutation permits self-parenting. The current job method permits restarting a completed job. Share expiry equality is unusable. Domain Touch does not advance Version. Each statement has a narrow source-level scope.

## Generate a fresh reference workspace

Run the following from the Netdrop root with Python and the .NET 10 SDK already available. The generator does not install either tool. It uses no third-party test package and writes a NuGet configuration with no package sources in the generated workspace. Choose a new output path each time; existing, relative, and in-project outputs are refused.

```powershell
$labOutput = Join-Path $env:TEMP ('netdrop-lifecycle-' + [guid]::NewGuid().ToString('N'))
python .\astraupskill\extended-course\lab\generate_lifecycle_lab.py --output $labOutput --mode reference
dotnet run --project (Join-Path $labOutput 'Lab.csproj')
```

Reference mode should report six passed observations and exit zero. SOURCE-MAP.json records the current source hashes and copied hashes. Inspect that manifest before interpreting results. If the application source changes later, this generator copies the changed source rather than silently restoring an old snapshot. A changed prediction should prompt review of the new behavior, not an automatic application rollback.

## Introduce one compatible defect

Starter mode changes only the copied ShareLink expiry comparison from strict greater-than to greater-than-or-equal. Method signatures and the surrounding source remain compatible. The generator checks that its exact anchor appears once before writing the workspace. If that assumption becomes false, it refuses to invent a mutation against unfamiliar source.

```powershell
$starterOutput = Join-Path $env:TEMP ('netdrop-lifecycle-starter-' + [guid]::NewGuid().ToString('N'))
python .\astraupskill\extended-course\lab\generate_lifecycle_lab.py --output $starterOutput --mode starter
dotnet run --project (Join-Path $starterOutput 'Lab.csproj')
```

The expected starter result is five passed observations and one failed expiry-equality observation, with a nonzero exit. This is intentional. Repair only the copied ShareLink.cs by restoring the strict comparison, then rerun the generated project. Do not change the test to accept the broken boundary and do not edit the original application file. The repaired copy should return to six passing observations.

## Explain what the lab cannot establish

The folder test invokes the domain method directly. It does not execute an authenticated HTTP request or prove a deployed exploit. The job test calls methods directly; it does not run the hosted worker or a durable scheduler. The Version test proves that this domain mutation does not increment the property; it does not inspect every possible database mechanism or migration.

Similarly, the metadata test observes retained text without querying search or executing a download. The earlier chapters explain how those paths use related fields, but this small executable does not run them. Keeping these limitations explicit makes the lab useful: it isolates facts that can be established quickly without pretending to cover the whole application.

## Exercise ND-21A: distinguish observation from requirement

Classify each of the six tests as a current characterization, a desired boundary, or both. For two characterizations that a capstone would change, write replacement acceptance tests in words. Explain why keeping the old characterization unchanged would obstruct the intentional behavior change.

## Exercise ND-21B: repair and explain the starter

Record the failing case, source-map mutation, copied-file repair, and final exit code. Explain why testing one tick before expiry and exactly at expiry distinguishes the two comparisons. State why a test using only a time well before expiry would miss the defect.

## Exercise ND-21C: present the final evidence ladder

Choose one capstone invariant and connect a domain test, a deterministic concurrency model, a provider-backed test, and a hosted request test to it. Name the claim each layer proves and one claim it leaves open. Submit this evidence ladder with the capstone review packet rather than claiming the small lab validates the complete system.

## Separate final answers

The [final lab review](review-07-lifecycle-lab.md) contains the answer key. Keep the original source and the generated workspace distinct in your report. The course is complete when you can explain both a successful test and its boundary of evidence, including what a proposed next implementation would need to change.
