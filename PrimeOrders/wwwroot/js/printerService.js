// Printer service functions
window.printToPrinter = function (content) {
	try {
		// Create a hidden iframe for printing
		const iframe = document.createElement('iframe');
		iframe.style.display = 'none';
		document.body.appendChild(iframe);

		// Write content to iframe with compact styling
		iframe.contentDocument.write(`
            <html>
                <head>
                    <style>
                        @page {
                            margin: 0;
                            size: 80mm auto;  /* Width for thermal paper */
                        }
                        body {
                            font-family: monospace;
                            font-size: 12px;
                            margin: 0;
                            padding: 3mm;
                        }
                        pre {
                            white-space: pre-wrap;
                        }
                    </style>
                </head>
                <body>
                    <pre>${content}</pre>
                </body>
            </html>
        `);

		// Print the iframe content
		iframe.contentWindow.print();

		// Remove the iframe after printing
		setTimeout(() => {
			document.body.removeChild(iframe);
		}, 1000);

		return true;
	} catch (error) {
		console.error('Printing failed:', error);
		return false;
	}
};


/*
// Create content to print
string printContent = $"Counter Value: {currentCount}\n" +
                     $"Date: {DateTime.Now}\n" +
                     "--------------------------------\n" +
                     "Thank you for using our service!";

// Call JavaScript function to handle printing
await JSRuntime.InvokeVoidAsync("printToPrinter", printContent);
*/