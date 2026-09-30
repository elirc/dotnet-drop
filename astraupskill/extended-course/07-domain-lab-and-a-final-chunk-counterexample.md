# 07. A domain lab and a final-chunk counterexample

The [domain lab generator](lab/generate_domain_lab.py) copies current Netdrop domain source into a new external project. The copied UploadSession and UserStorageAccount execute their real constructors and methods; no handwritten Python substitute decides their outcomes. Starter mode changes only the final positive chunk-size calculation in the disposable copy, leaving signatures compatible with current source.

This lab has a deliberately narrow claim. It does not execute EF handlers, filesystem publication, queue delivery, or the HTTP validation pipeline. It tests domain byte partitioning, hash-input syntax, expiry equality, and read-only quota preflight. Those observations support the first chapters while leaving the completion recovery capstone as proposed work.

## Use a fresh external directory

Run from the Netdrop root with Python and an installed .NET 10 SDK. The generator refuses existing, relative, and in-project output paths. It copies only the domain files required by the harness and writes a source manifest. Package sources are cleared in the generated SDK-only project, so the lab does not restore the application's dependency graph.

```powershell
$uploadReference = Join-Path $env:TEMP ("netdrop-reference-" + [guid]::NewGuid().ToString('N'))
python astraupskill/extended-course/lab/generate_domain_lab.py --output $uploadReference --mode reference
dotnet run --project (Join-Path $uploadReference 'Lab.csproj')

$uploadStarter = Join-Path $env:TEMP ("netdrop-starter-" + [guid]::NewGuid().ToString('N'))
python astraupskill/extended-course/lab/generate_domain_lab.py --output $uploadStarter --mode starter
dotnet run --project (Join-Path $uploadStarter 'Lab.csproj')
```

If a suitable SDK is unavailable, record that setup limit. Do not interpret a missing reference pack or compilation error as evidence that the intended chunk assertion detected a defect. The useful failure in starter mode occurs after the program compiles and runs its named checks.

## Why the exact-multiple fixture is insufficient

The mutation returns ChunkSize for every positive-size chunk instead of taking the minimum with the remaining bytes. Eight bytes in chunks of four still appears correct: both expected chunks are four. Zero bytes also remains correct because the earlier special case returns zero before the mutated expression. Those passing fixtures can conceal the defect.

Ten bytes in chunks of four is the discriminating case. The final chunk must be two, but the starter says four. A client following that boundary would be asked to provide more bytes than the declared total. The test lists all three expected sizes, so the failure identifies a violated partition rather than merely a changed number.

The reference reports six passes. The starter reports five passes and one intended failure for the final remainder. The other cases cover an empty file's single zero chunk, exact multiples, rejection of raw hash whitespace, expiry equality, and preflight that leaves usage unchanged. They show why a suite needs boundaries that differ in relevant ways instead of many equivalent happy paths.

## Preserve the distinction between preflight and reserve

The quota check creates an account with ten bytes of capacity, calls EnsureCanReserve for seven, and verifies UsedBytes remains zero. It then reserves seven, asks whether another four can fit, expects rejection, and verifies usage remains seven. This sequence makes mutation timing visible without a database.

It does not establish concurrent quota safety. Two separate persistence contexts are not represented, and no database transaction executes. The check proves the domain method's local behavior. Use that fact in the completion ledger, then identify the integration evidence needed for a stronger shared-capacity claim.

## Repair and explain the copy

Restore the Math.Min expression in the generated starter's UploadSession.cs. Rerun the lab and record the actual result. Preserve the source map and add a repair note if needed. Do not overwrite the application's domain file or replace it with an old snapshot from another course revision.

A different correct arithmetic expression can be acceptable if it handles every valid index and cannot overflow under the constructor's accepted range. Explain its proof using total size, offset, and remaining bytes. A repair that special-cases only the ten/four fixture has learned the test rather than the invariant. Add another nonmultiple example to challenge that shortcut.

The generator applies its mutation only if the exact expected source expression appears once. A refusal after source evolution is useful evidence that the exercise needs review. It should not be bypassed by replacing a random return statement or copying a stale historical implementation over the current project.

## Exercise ND-07A: predict the six cases

Write each expected domain result before execution and identify the sole intended starter failure. Explain why zero and exact multiples survive the defect. Include the distinction between compilation success and semantic assertion failure in your result receipt.

## Exercise ND-07B: generalize the repair

Repair the disposable source and add one nonmultiple total/chunk pair unlike ten/four. Prove that expected sizes sum to total size and every valid index has a nonnegative size. Explain why the constructor's chunk-count bound matters for the offset calculation.

## Exercise ND-07C: resist an evidence overclaim

A learner says six passing checks prove upload completion cannot double-charge quota. Write a correction that preserves the value of the lab while naming its missing handler, database, and concurrency boundaries. Propose the smallest additional experiment that addresses one of those boundaries without pretending it covers all of them.

Consult the [separate lab review](review-03-domain-lab.md) after recording your predictions and observations.
