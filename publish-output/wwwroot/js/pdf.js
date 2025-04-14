window.openPdfInNewWindow = function (dataUrl) {
    const pdfWindow = window.open();
    pdfWindow.document.write(`
        <html>
            <head>
                <title>Concern Details</title>
                <style>
                    body, html {
                        margin: 0;
                        padding: 0;
                        height: 100%;
                        overflow: hidden;
                    }
                    iframe {
                        width: 100%;
                        height: 100%;
                        border: none;
                    }
                </style>
            </head>
            <body>
                <iframe src="${dataUrl}"></iframe>
            </body>
        </html>
    `);
}; 