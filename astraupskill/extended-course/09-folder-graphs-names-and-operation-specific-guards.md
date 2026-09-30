# 09. Folder graphs, names, and operation-specific guards

File operations often share a service but enforce different preconditions. Netdrop's create, rename, move, copy, delete, and restore methods illustrate why a reviewer must trace each path rather than assume one helper protects the entire namespace. This chapter studies graph integrity and naming policy using only owned synthetic fixtures. It proposes improvements without modifying application behavior.

Read [FileService](../../src/NetDropbox.Application/Files/FileService.cs), [FileNode](../../src/NetDropbox.Domain/FileNode.cs), and [NetDropboxDbContext](../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs). The domain cleans names by trimming, rejecting blank values, and rejecting path separators. It does not decide uniqueness among siblings. The service and persistence model must own that broader namespace policy if the product requires it.

## A name check and a unique constraint are different

CreateFolderAsync first queries whether a nondeleted node with the supplied owner, parent, and raw name exists. It then constructs a folder, whose name is trimmed, and saves. The file-node index over owner, parent, name, and deleted flag is not declared unique in the inspected model. Therefore the preliminary check is not a database-enforced uniqueness guarantee under competing requests.

Normalization adds another issue. A raw request with surrounding spaces can be checked against existing untrimmed equality and then stored trimmed. The result can differ from the name used in the existence query. Provider collation can affect case comparison as well. A complete name policy must specify normalization and comparison at the authoritative boundary, not merely perform a convenient check before construction.

RenameAsync loads an owned nondeleted node, invokes Rename, and saves. It does not perform the create method's sibling-name existence query. A course should not claim all operations reject duplicate names because one create path checks them. If uniqueness is intended, the invariant must cover every mutation that can change the relevant tuple.

## Parent references form a graph

MoveAsync loads the source through GetOwnedNode. When a parent is supplied, it loads that parent under the same owner and requires Folder type. This protects a local destination ownership/type condition. It does not traverse descendants to reject moving a folder beneath itself or one of its descendants.

FileNode.Move contains a self-containment guard only when the node is a File. Since a file cannot be the destination folder in the service path, that guard is not a complete folder-cycle policy. A folder can form a cycle under the inspected domain and service checks unless another uninspected boundary prevents the call. State the source path precisely rather than generalizing from the word tree.

Use a tiny graph: root contains A, and A contains B. Moving A under B creates a cycle A to B to A. A direct self-parent case is an even smaller fixture. Both require a graph invariant, not only a nonempty identifier or ownership comparison. A future traversal that assumes acyclicity can loop or omit nodes if that invariant is not enforced.

## Create and copy have different destination checks

CreateFolderAsync does not perform the same owned-parent lookup that MoveAsync does. CopyAsync loads an owned file and its versions, obtains the current blob, adds a reference, creates a new FileNode under the supplied parent, and saves. It does not validate that destination parent with MoveAsync's helper in this method. These are operation-specific observations, not proof that every HTTP route is unguarded.

A proposed shared destination policy could centralize parent existence, ownership, folder type, deletion state, and cycle rules where applicable. It should distinguish operations: creating a child cannot create the same cycle as moving an existing ancestor, while copying a file has no descendant subtree. Reusing one helper is useful only if its contract fits each operation.

Avoid a refactor that changes behavior accidentally while merely trying to reduce duplication. First write a matrix of required guards for create, move, copy, rename, restore, and upload completion. Then identify missing or inconsistent rules. A helper should implement an agreed policy, not hide unresolved differences behind a generic ValidateNode name.

## Trash is a state, not immediate deletion

DeleteAsync marks the node deleted and saves. It does not recursively delete descendants in this method, remove blob bytes, or release quota. RestoreAsync loads an owned node including deleted ones, clears the deleted state, and saves. It does not check for a new sibling occupying the same name before restoration.

These choices require a product interpretation. If a folder is trashed, are its descendants effectively hidden through navigation, individually marked deleted, or still accessible by known identifiers? The source methods alone show a node-level state change. A complete subtree policy requires reading list, download, sharing, and cleanup paths together. Later chapters will connect those paths without assuming one flag propagates automatically.

Restoration can collide with names created while the original node was in trash. A policy might reject, rename, merge, or allow duplicate names. The current nonunique index does not choose among those user experiences. A test should assert the chosen behavior explicitly rather than relying on whichever row a later query happens to return first.

## Copying content also copies assumptions

CopyAsync creates a version pointing to the same blob, then marks the copy clean using the source node's extracted text and thumbnail. It does not invoke the scanner in this method. It also does not check source ScanStatus before calling MarkScanClean. A source-grounded review should identify this path and ask whether reusing derived evidence is valid for every source state.

A safe reuse policy might require verified clean evidence for the exact copied version. If a source is pending, failed, or infected, marking a new node clean could contradict the intended download policy. This is a design concern supported by the inspected call sequence; demonstrating a complete externally reachable incident still requires the endpoint, permissions, and storage fixture.

The blob reference count increment and copy creation occur in one context before saving, but concurrent reference accounting still needs provider evidence. A successful copy should not be confused with physically duplicating the bytes. It creates another logical reference to content, which has consequences for quota and retention policies discussed later.

## Exercise ND-09A: build a guard matrix

For create, rename, move, copy, delete, and restore, list the observed owner, parent, name, deletion, and scan checks. Mark absent checks as source observations, not automatically as confirmed remote defects. Identify one invariant that needs a shared policy across several operations.

## Exercise ND-09B: construct two graph counterexamples

Draw a self-parent folder move and an ancestor-under-descendant move. Trace which current checks pass. Propose a cycle-detection strategy and a database-aware concurrent test where two independent moves could jointly create a cycle. Explain why validating each move against an old snapshot may be insufficient.

## Exercise ND-09C: review copied scan evidence

Create a proposed test matrix for copying a clean, pending, failed, and infected source. State the current method's observable transition and a safer product contract you would recommend. Include exact version identity in the evidence rather than relying only on the node's current status label.

## Review checkpoint

A strong answer reads each operation independently, then compares their policies. It distinguishes name syntax from uniqueness, parent ownership from acyclicity, and logical copy from physical duplication. Its improvement proposal is based on explicit invariants and discriminating fixtures rather than a broad assertion that CRUD operations should all behave alike.
