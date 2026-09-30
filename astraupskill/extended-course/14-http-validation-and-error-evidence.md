# 14. HTTP validation and error evidence

A direct handler call is not the same execution path as an authenticated HTTP request. Netdrop uses controllers, identity extraction, MediatR validation, application exceptions, and middleware to construct public behavior. This chapter maps those layers so an exercise can claim exactly what it tested. It does not run the host or expose any live configuration values.

Read [UploadsController](../../src/NetDropbox.Api/Controllers/UploadsController.cs), [FilesController](../../src/NetDropbox.Api/Controllers/FilesController.cs), [Program](../../src/NetDropbox.Api/Program.cs), [ValidationBehavior](../../src/NetDropbox.Application/Common/ValidationBehavior.cs), and [ExceptionMiddleware](../../src/NetDropbox.Api/Support/ExceptionMiddleware.cs). The upload and file controllers declare authorization. Upload commands obtain UserId from the principal rather than accepting it in the request body.

## Follow one start request through layers

An HTTP request must route and bind before the controller sends a StartUploadCommand. Authentication and authorization are configured in the host. The controller derives the acting user through its User helper, then MediatR invokes registered validators through ValidationBehavior before the handler. The handler performs database and domain work, and exceptions can be translated by middleware.

Calling StartUploadHandler.Handle directly bypasses controller binding and the MediatR pipeline. Its domain constructor may still reject invalid values, but request-validator limits such as the chunk-size interval are not automatically executed. A test that directly calls the handler with a tiny chunk size should not be described as proof that the public API accepts that size.

Similarly, directly constructing a controller does not execute the authorization middleware merely because attributes exist on its class. A hosted test is needed to exercise the configured pipeline. The source declaration is evidence of intended metadata; the runtime host establishes how a request is admitted under that configuration.

## Request size and chunk size are distinct

The chunk upload action declares a request-size limit of sixty-four megabytes. StartUploadValidator permits a smaller configured chunk-size range. The filesystem adapter then enforces the session's exact expected size for the particular index. These are three separate bounds: transport body ceiling, accepted session configuration, and exact chunk content length.

A body can be below the transport ceiling but still too large for its session index. A final remainder chunk can be much smaller than the configured chunk size. A correct test matrix includes each boundary rather than assuming one attribute makes the others redundant. Hosted tests are needed for transport enforcement, while adapter tests can verify exact byte acceptance independently.

The checksum arrives through a named header. Its validator checks nonempty length, while actual storage compares computed digest. A syntactically plausible header does not prove body integrity. The pipeline should be explained as layered rejection opportunities with different evidence and costs.

## Exceptions have a public translation

ExceptionMiddleware maps AppException to its selected status, DomainException to bad request, UnauthorizedAccessException to unauthorized, and other exceptions to an internal-server-error category. For server errors it hides detailed exception text from the ProblemDetails body while logging. This is a concrete mapping, not a universal translation of every database or arithmetic issue into a business conflict.

For example, a missing row queried with SingleAsync can raise a different exception from an explicit AppException not-found branch. The upload Status action uses SingleAsync under session/owner predicates. A direct service or context test should record its exception, while a hosted test can verify how the middleware presents it. Do not rename every absence as HTTP 404 without following the actual branch.

ValidationBehavior aggregates validator failures into an AppException before calling the next handler. It invokes the synchronous Validate method on each registered validator. The course should not claim it automatically runs arbitrary asynchronous validation rules or that its token parameter makes every validator cancellable. Read the invoked API and the actual validators when extending that path.

## Response execution is later work

Download actions obtain a stream and return a File result with range processing enabled. The action's successful return does not mean the client has received all bytes. Framework result execution, stream reads, network transport, and client cancellation occur afterward. A service test can prove which stream was opened but not completed delivery.

The inspected exception middleware does not contain an explicit Response.HasStarted guard before assigning status and writing JSON. A late stream failure may therefore interact with an already-started response differently from an early application exception. This is a source concern requiring a hosted streaming fixture, not a reason to invent a guaranteed final status from direct method behavior.

Range processing adds another distinction. A client can request part of a file, and the result executor's behavior depends on the stream and framework. The service's AccessCount increment does not distinguish complete versus partial transfers. If product metrics or download limits depend on transferred bytes, a separate observation mechanism is needed.

## Public shares have their own admission path

PublicSharesController is explicitly anonymous and applies the named public rate-limit policy to its download action. The service still checks token usability, optional password, permission, and scan status. Anonymous routing does not mean absence of all access controls; the token and its conditions form a different authority mechanism from the owner JWT route.

The host's public limiter partitions by remote IP with a fixed-window policy. That source fact does not establish deployment behavior behind every proxy or a distributed rate limit across replicas. A future operational review must inspect trusted forwarding and deployment topology before making those claims. The bounded course uses synthetic requests and avoids live tokens or infrastructure changes.

## Exercise ND-14A: build an execution ladder

For chunk-size validation, exact body length, owner identity, and public error shape, identify the smallest test layer that executes the relevant mechanism. Explain what a direct handler test bypasses and what a hosted test with fake storage still cannot establish about filesystem publication.

## Exercise ND-14B: classify absence and failure

Compare an explicit not-found AppException, a SingleAsync missing-row exception, a DomainException, and a stream failure after response execution begins. Predict only what the inspected mapping supports. Identify where a hosted fixture is required before assigning a final HTTP observation.

## Exercise ND-14C: design a bounded upload request matrix

Include a valid full chunk, a valid final remainder, an oversized-for-session body below transport maximum, a malformed checksum header, and an unauthenticated request. State the expected rejecting layer and a downstream collaborator that should not run where applicable. Keep expected outcomes distinct from unexecuted predictions.

## Review checkpoint

A strong answer treats attributes, middleware, validators, handlers, and stream execution as separate evidence boundaries. It neither dismisses direct tests nor overstates them. It can name the exact layer that must run to establish each public claim and report a setup failure without pretending an HTTP contract was tested.
