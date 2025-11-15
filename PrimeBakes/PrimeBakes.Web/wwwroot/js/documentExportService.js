// Generic file download functionality for all file types
window.saveFile = function (base64String, filename, mimeType = null) {
	const byteCharacters = atob(base64String);
	const byteArrays = [];

	// Convert the base64 string to byte array
	for (let i = 0; i < byteCharacters.length; i++) {
		byteArrays.push(byteCharacters.charCodeAt(i));
	}
	const byteArray = new Uint8Array(byteArrays);

	// Auto-detect MIME type based on file extension if not provided
	let contentType = mimeType;
	if (!contentType) {
		const extension = filename.split('.').pop().toLowerCase();
		const mimeTypes = {
			// Documents
			'pdf': 'application/pdf',
			'doc': 'application/msword',
			'docx': 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',

			// Spreadsheets
			'xls': 'application/vnd.ms-excel',
			'xlsx': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
			'csv': 'text/csv',

			// Images
			'jpg': 'image/jpeg',
			'jpeg': 'image/jpeg',
			'png': 'image/png',
			'gif': 'image/gif',
			'bmp': 'image/bmp',
			'svg': 'image/svg+xml',
			'webp': 'image/webp',

			// Archives
			'zip': 'application/zip',
			'rar': 'application/x-rar-compressed',
			'7z': 'application/x-7z-compressed',

			// Text
			'txt': 'text/plain',
			'json': 'application/json',
			'xml': 'application/xml',

			// Other
			'ppt': 'application/vnd.ms-powerpoint',
			'pptx': 'application/vnd.openxmlformats-officedocument.presentationml.presentation'
		};

		contentType = mimeTypes[extension] || 'application/octet-stream';
	}

	// Create a blob from the byte array
	const blob = new Blob([byteArray], { type: contentType });

	// Create a download link and trigger the download
	const link = document.createElement('a');
	link.href = URL.createObjectURL(blob);
	link.download = filename;
	document.body.appendChild(link);
	link.click();
	document.body.removeChild(link);

	// Clean up the object URL
	setTimeout(() => {
		URL.revokeObjectURL(link.href);
	}, 100);
};