// Printer service functions
window.printToPrinter = function (content) {
    try {
        // Create a hidden iframe for printing
        const iframe = document.createElement('iframe');
        iframe.style.display = 'none';
        document.body.appendChild(iframe);

        // Write content to iframe with enhanced thermal receipt styling
        iframe.contentDocument.write(`
            <html>
                <head>
                    <style>
                        @page {
                            margin: 0;
                            size: 80mm auto;  /* Width for thermal paper */
                        }
                        body {
                            font-family: 'Arial', 'Helvetica', sans-serif;
                            font-size: 14px;
                            margin: 0;
                            padding: 8mm;
                            width: 64mm;
                            color: #000000;
                            line-height: 1.5;
                            -webkit-print-color-adjust: exact;
                            print-color-adjust: exact;
                        }
                        
                        /* Header */
                        .header {
                            text-align: center;
                            margin-bottom: 12px;
                        }
                        .company-name {
                            font-family: Arial, sans-serif;
                            font-size: 25px;
                            font-weight: bold;
                            margin-bottom: 0px;
                        }
                        .header-line {
                            font-family: Arial, sans-serif;
                            font-size: 15px;
                            font-weight: bold;
                            margin-top: 5px;
                        }
                        
                        /* Separator */
                        .bold-separator {
                            border-bottom: 2px dashed #000000;
                            margin: 8px 0;
                            height: 1px;
                        }
                        
                        /* Bill Details */
                        .bill-details {
                            font-family: 'Courier New', monospace;
                            font-size: 15px;
                            font-weight: bold;
                            padding: 2px;
                            margin-bottom: 8px;
                        }
                        .detail-row {
                            margin: 3px 0;
                        }
                        .detail-label {
                            font-weight: bold;
                        }
                        .detail-value {
                            font-weight: bold;
                        }
                        
                        /* Items Table */
                        .items-table {
                            width: 100%;
                            border-collapse: collapse;
                            margin: 2px 0;
                        }
                        .table-header th {
                            font-family: Arial, sans-serif;
                            font-size: 14px;
                            font-weight: bold;
                            padding: 5px 2px;
                            border-bottom: 1px solid #000000;
                        }
                        .table-row td {
                            font-family: 'Courier New', monospace;
                            font-size: 12px;
                            font-weight: bold;
                            padding: 5px 2px;
                        }
                        
                        /* Summary Table */
                        .summary-table {
                            font-family: 'Courier New', monospace;
                            font-size: 15px;
                            font-weight: bold;
                            width: 100%;
                            padding: 2px;
                        }
                        .summary-table td {
                            padding: 3px 2px;
                        }
                        .summary-label {
                            font-weight: bold;
                        }
                        .summary-value {
                            font-weight: bold;
                        }
                        
                        /* Grand Total */
                        .grand-total {
                            width: 100%;
                            font-family: 'Arial Black', 'Arial', sans-serif;
                            font-size: 16px;
                            font-weight: bold;
                            padding: 2px;
                            margin: 5px 0;
                        }
                        .grand-total td {
                            padding: 3px 2px;
                        }
                        .grand-total-label {
                            font-weight: bold;
                        }
                        .grand-total-value {
                            font-weight: bold;
                        }
                        
                        /* Amount in Words */
                        .amount-words {
                            font-family: 'Arial', sans-serif;
                            font-size: 13px;
                            font-weight: bold;
                            text-align: center;
                            padding: 8px 0;
                            font-style: italic;
                        }
                        
                        /* Footer */
                        .footer-timestamp {
                            font-family: 'Courier New', monospace;
                            font-size: 12px;
                            font-weight: bold;
                            text-align: center;
                            margin: 2px 0;
                            padding: 2px;
                        }
                        .footer-text {
                            font-family: 'Courier New', monospace;
                            font-size: 13px;
                            font-weight: bold;
                            text-align: center;
                            padding: 2px;
                        }
                    </style>
                </head>
                <body>
                    ${content}
                </body>
            </html>
        `);

        // Print the iframe content with delay to ensure proper rendering
        setTimeout(() => {
            iframe.contentWindow.print();

            // Remove the iframe after printing
            setTimeout(() => {
                document.body.removeChild(iframe);
            }, 1000);
        }, 300);

        return true;
    } catch (error) {
        console.error('Printing failed:', error);
        return false;
    }
};