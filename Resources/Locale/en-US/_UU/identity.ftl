ent-PhotoCamera = photo camera
    .desc = A simple camera for passport photos on work IDs.

ent-WorkPhoto = photograph
    .desc = A portrait photograph. It can be attached to a work ID.

photo-camera-no-target = You can only photograph people.
photo-camera-taken = The photo is ready.
photo-camera-taken-floor = The photo is ready, but it fell on the floor.
work-photo-name = photograph — {$name}

ent-PositionScanner = position scanner
    .desc = Reads a work ID and records the position, tag and name in the state registry.

ent-WorkRegistryConsole = positions console
    .desc = The registry of the state's positions and tags. Positions are revoked through it: the card stays, but its tag no longer applies.

ent-WorkRegistryStamp = state registry seal
    .desc = Pressed onto a fully signed work ID. Without the seal the position scanner will not read the card.

# --- work ID ---
work-card-issued-name = work ID: {$holder}
work-card-issued-desc = A plastic card with a position, signatures and the holder's photograph.
work-card-window-title = Work ID
work-card-holder = Holder:
work-card-position = Position:
work-card-employer = Employer:
work-card-workplace = Workplace:
work-card-tag = Work tag:
work-card-date = Date:
work-card-signatures = Signatures:
work-card-signature = Signature:
work-card-signatures-state = signed on behalf of the state
work-card-photo = Photo
work-card-unknown = not specified
work-card-no-tag = none
work-card-unsigned = unsigned
work-card-no-photo = no photograph
work-card-revoked = TAG REVOKED
work-card-photo-attached = The photograph is attached to the work ID.
work-card-no-photo-to-attach = Hold a photograph in your other hand.
work-card-footer = A work ID is issued from a signed contract. The tag applies until it is revoked from the positions console.
work-card-stamp = Registry seal:
work-card-stamped = {$name}
work-card-no-stamp = no seal
registry-stamp-applied = The registry seal is pressed on: {$name}.
registry-stamp-no-state = no state
registry-stamp-already = The card already bears the registry seal.
registry-stamp-not-signed = The seal goes only on a fully signed work ID.
work-id-examine-stamped = [color=gold]Registry seal:[/color] {$name}
work-id-examine-no-stamp = [color=#ff5555]No registry seal.[/color]
work-id-verb-open = Open work ID
work-id-examine-revoked = [color=#ff5555]The tag was revoked by the state.[/color]

# --- positions console ---
work-registry-title = Positions console
work-registry-unbound = Bind it to a state first.
work-registry-unbound-short = Not bound to a state
work-registry-faction = State: {$name}
work-registry-summary = Entries: {$count}, tags: {$tags}
work-registry-empty = The registry is empty. Scan a work ID with the position scanner.
work-registry-entry-details = tag: {$tag} · appointed by: {$employer} · {$date}
work-registry-fire = Dismiss
work-registry-restore = Restore
work-registry-fired = {$name} has been dismissed: the tag no longer applies.
work-registry-restored = {$name} has been restored: the tag applies again.
work-registry-entry-missing = Entry not found.
work-registry-access-button = Configure access
work-registry-access-title = Console access
work-registry-access-use = Can use
work-registry-access-edit = Can edit
work-registry-access-no-tags = No tags yet: scan the first work ID.
work-registry-access-footer = An empty list on the left lets everyone in. Tags come from scanned work IDs.
work-registry-access-saved = Access lists updated.
work-registry-no-power = The console has no power.
work-registry-no-access = You do not have access to the console.
work-registry-save = Save
work-registry-cancel = Cancel
work-registry-close = Close
work-registry-footer = Dismissal does not damage the card: the tag simply stops applying on consoles and work stations.

position-scanner-unbound = Bind the scanner to a state first.
position-scanner-no-card = Hold a work ID in your other hand.
position-scanner-empty = The work ID has no name or position.
position-scanner-no-stamp = The work ID bears no registry seal.
position-scanner-scanned = {$name} — position "{$position}" recorded in the registry.

uu-construction-step-paper = a sheet of paper

ent-TypewriterKit = typewriter parts box
    .desc = Parts to assemble a typewriter. Open the box to get the materials.

kit-box-unpacked = You pry the box open and shake out its contents.

position-scanner-wrong-state = The registry stamp belongs to another state.
registry-stamp-wrong-state = The card is signed on behalf of another state.

faction-bound-verb = Bind to a state
faction-bound-done = Bound to the state "{$name}".
faction-bound-already = Already bound to a state.
faction-bound-no-state = You need a state to bind it.

position-scanner-revoked = The tag is revoked: this card no longer applies.
work-card-revoked-by = TAG REVOKED: {$state}, {$date}
