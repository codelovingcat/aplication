# CV Upload Screen Specification

## Purpose

The first screen of the CV intake flow lets a candidate provide a CV and optional context before analysis.

## User flow

1. Select or drag a PDF/DOCX CV into the upload area.
2. Show the selected file name and upload status.
3. Optionally enter additional information that is not present in the CV.
4. Validate the file before allowing the analysis step to continue.
5. Start CV analysis and display a loading state while processing.

## UI states

### Empty

- Upload area is visible.
- Accepted formats are shown as PDF / DOCX.
- The additional-information field is optional.
- The analyze/continue action is disabled until a valid CV is selected.

### File selected

- Show the file name and size.
- Allow the candidate to replace the selected file.
- Show validation feedback immediately when possible.

### Validation error

Display a concise error for:

- Unsupported extension.
- Invalid content type.
- Empty or corrupted file.
- File exceeding the configured size limit.
- Unsafe file name.

The candidate should be able to select another file without restarting the flow.

### Processing

- Disable duplicate submissions.
- Show a clear loading/progress state.
- Preserve the selected file and additional information in the current form state.

### Success

Continue to the CV analysis result or the next intake step.

## Accessibility

- The upload control must be keyboard accessible.
- Error messages should be associated with the relevant form control.
- Status changes should be announced to assistive technologies.
- Drag-and-drop must have a keyboard-accessible alternative.

## Related issue

This specification addresses issue #10.
