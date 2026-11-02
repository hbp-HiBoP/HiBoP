# Synthetic iEEG fixture on MNI anatomy

The `QuestAnatomyFixtureTests.IEEGArchive_LoadsRawSyntheticUnitsAndExactPreparedSamples`
test automatically constructs this fixture in its temporary directory with
[SyntheticSceneProjectFixture](../../../Assets/Tests/PlayMode/HBP.PlayModeTestUtilities/SyntheticSceneProjectFixture.cs).
Sources are the VISU/FACE protocol and
[synthetic MNI contacts](mni-contacts/contacts-fixture.md), all versioned.
No Python preparation, old Player or `.artifacts` input is needed.

Generation produces the archive, protocol and two BrainVision triplets
`synthetic-left/right.vhdr/.vmrk/.eeg`. The protocol is installed into the test's
isolated state before project loading: a HiBoP archive does not replace
global protocols. Generated files and reports are temporary.
The VISU FACE window (-500 to 1000 ms) and `None` normalization are retained.
No acquired signal is used. Earlier Player capture procedures remain
available in Git, along with the historical results below.

The `MNI iEEG` visualization contains a `Synthetic uV` column, the eight MNI contacts
described in the contact fixture and 151 prepared samples at 100 Hz. Each patient has
channels A1, A2 and B1; B2 is missing and masked. A2 of the right patient is
blacklisted. In each file, event S20 is at file sample 100
(BrainVision position 101), that is prepared index **50**, local time **0 ms**.

| Contact per patient | Value at 0 ms | Unit | Availability |
| --- | ---: | --- | --- |
| A1 | -1 | uV | Available |
| A2 | 0 | uV | Available; blacklisted on the right |
| B1 | +1 | uV | Available |
| B2 | Replacement 0 | Unspecified | Missing, masked |

Preparation also contains -10 and +10 values; serialized ranges remain
**SpanMin=-10, Middle=0, SpanMax=10** at all time points. At index 75
(250 ms), A1/B1 are -1.25/+1.25. The generator explicitly defines each sample and
fixes ZIP dates; provenance of qualified artifacts is in the
[iEEG comparison context](../scientific-parity/ieeg/measurement-context.json).

The single-column fixture is aligned (temporal Alpha=0). Capture tests also use
200 Hz navigation/100 Hz projection: at 15 ms, projection index 1 and Alpha=0.5.
The surface retains the input at index 1, while sites use exactly
`TemporalSample.Evaluate`. Floor/Round policies remain those of Desktop and
are explicitly transported; transfer adds no rounding.
`ActivityAlpha` is another parameter: opacity.
