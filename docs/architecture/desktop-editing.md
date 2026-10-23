# Desktop element editing

The visualizer keeps diagram XML read-only while the side panel edits
application metadata. The panel opens for tasks, gateways, start/end events,
and other supported BPMN business elements. Task panels expose Input, Output,
Recursos necessários, Instruções, Descrição, and attachments; non-task
elements expose Descrição and attachments. A metadata save is separate from an
attachment upload: upload confirmation invalidates only the task query and
does not mark text fields as dirty.

The selected element and type are supplied by the BPMN viewer. Markers indicate
whether a supported element has any metadata, and the unsaved badge is shown
only for pending text changes. Guest users see the same values and download
actions but cannot mutate them.
