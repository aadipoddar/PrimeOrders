window.setupSalePageKeyboardHandlers = (dotNetHelper) => {
	// Remove existing listeners first
	if (window.salePageKeyHandler) {
		document.removeEventListener('keydown', window.salePageKeyHandler);
	}

	// Create new handler
	window.salePageKeyHandler = async (event) => {
		// Don't handle keys when typing in input fields
		if (['INPUT', 'TEXTAREA', 'SELECT'].includes(event.target.tagName)) {
			return;
		}

		try {
			await dotNetHelper.invokeMethodAsync('HandleKeyboardShortcut',
				event.key);
		} catch (error) {
			console.error('Keyboard handler error:', error);
		}

		// Prevent default for function keys
		if (event.key.startsWith('F') || event.ctrlKey) {
			event.preventDefault();
		}
	};

	// Add new listener
	document.addEventListener('keydown', window.salePageKeyHandler);
};

window.showProductSearchIndicator = (searchText) => {
	const indicator = document.getElementById('productSearchIndicator');
	if (indicator) {
		indicator.style.display = 'block';
		document.getElementById('searchText').textContent = searchText;
	}
};

window.hideProductSearchIndicator = () => {
	const indicator = document.getElementById('productSearchIndicator');
	if (indicator) {
		indicator.style.display = 'none';
	}
};

window.updateProductSearchIndicator = (searchText, resultCount) => {
	document.getElementById('searchText').textContent = searchText;
	document.getElementById('searchResults').textContent = `${resultCount} products found`;
};