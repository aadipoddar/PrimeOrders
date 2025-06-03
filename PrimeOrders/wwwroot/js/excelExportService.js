// Excel download functionality
window.saveAs = function (base64String, filename) {
	const byteCharacters = atob(base64String);
	const byteArrays = [];

	// Convert the base64 string to byte array
	for (let i = 0; i < byteCharacters.length; i++) {
		byteArrays.push(byteCharacters.charCodeAt(i));
	}
	const byteArray = new Uint8Array(byteArrays);

	// Create a blob from the byte array
	const blob = new Blob([byteArray], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });

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