## Radio — Entity names
## Entity names/descriptions are resolved by the engine through ent-<ProtoId>
## (see Robust.Shared/Localization/LocalizationManager.Entity.cs), so radio-*-name
## style keys referenced from YAML would never apply.
ent-RadioTransmitter = Transmitter
    .desc = Creates and transmits on radio frequencies. Can only be configured once.

ent-RadioDevice = Radio
    .desc = Receives radio transmissions. Bind it to a transmitter to receive messages.

ent-RadioEarpiece = Earpiece
    .desc = A discreet earpiece for receiving radio messages. Bind it to a transmitter.

ent-FrequencyConfigurator = Frequency Configurator
    .desc = Copies frequencies from transmitters. Use on a transmitter to copy, then on a radio or earpiece to paste.

ent-SignalTower = Signal Tower
    .desc = Extends radio range for a specific frequency by 20 tiles.

ent-GeneralJammer = Jammer
    .desc = Jams all radio transmissions in range.

ent-SpecificJammer = Frequency Jammer
    .desc = Jams a specific radio frequency in range.


## Radio — UI
radio-transmitter-title = Transmitter
radio-no-frequency = No frequency set
radio-enter-frequency = Enter frequency (1-3 symbols):
radio-frequency-placeholder = e.g. ABC
radio-set-frequency = Set
radio-current-frequency = Current frequency: {$freq}
radio-invalid-frequency = Invalid frequency! Use 1-3 characters.
radio-set-success = Success!
radio-jammed = Transmission jammed!
radio-transmit-on = Transmit ON!
radio-transmit-off = Transmit OFF!
radio-enter-name = Frequency name (optional):
radio-name-placeholder = e.g. Command
radio-bound = Bound to frequency «{$frequency}»!
radio-already-bound = Already bound to frequency «{$frequency}».
radio-already-configured = Transmitter is already configured! Deconstruct it to reconfigure.
radio-configurator-copied = Frequency «{$frequency}» copied!
radio-configurator-pasted = Frequency «{$frequency}» transferred!
radio-configurator-empty = Configurator is empty! Copy a frequency from a transmitter.
radio-jammer-no-target = No target frequency. Tune it with a frequency configurator.
radio-jammer-target = Jamming frequency: {$frequency}
radio-jammer-frequency-set = Jammer tuned to frequency «{$frequency}».

radio-transmitter-footer = One-time setup. To change the frequency, deconstruct the transmitter.


## Signal tower
tower-window-title = Signal Tower
tower-enter-code = Access code:
tower-set-code = Access code (set it on first setup):
tower-code-placeholder = e.g. AXIS-141
tower-apply = Apply
tower-footer = Only a narrow circle knows the code. A configurator will not open a locked tower.
tower-invalid-code = Code must be 3-12 letters or digits.
tower-code-set = Code set. Remember it - only someone who knows the code can change it.
tower-wrong-code = Wrong access code.
tower-frequency-set = Tower set to frequency "{$freq}".
tower-locked = The tower is locked by a code - the configurator will not help.
tower-status-locked = Locked by code
tower-status-open = No code set