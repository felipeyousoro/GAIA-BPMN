# PDF printing

The desktop exposes **Imprimir** only when an attachment has the validated
`application/pdf` content type. The authenticated content endpoint checks the
PDF signature (`%PDF-`) before returning an inline response. Download requests
use `?download=true` and retain the original file name.

Printing opens the inline response in a controlled browser window and invokes
the system print dialog after the document loads. A blocked popup is reported
as an error instead of silently falling back to an unauthenticated URL.

The storage key is never sent to the desktop. Guest users use the same
authenticated route as Admin users and can print only attachments they are
authorized to read.
