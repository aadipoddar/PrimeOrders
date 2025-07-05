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

window.setupOrderPageKeyboardHandlers = (dotNetHelper) => {
	// Remove existing listeners first
	if (window.orderPageKeyHandler) {
		document.removeEventListener('keydown', window.orderPageKeyHandler);
	}

	// Create new handler
	window.orderPageKeyHandler = async (event) => {
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
	document.addEventListener('keydown', window.orderPageKeyHandler);
};

window.setupPurchasePageKeyboardHandlers = (dotNetHelper) => {
	// Remove existing listeners first
	if (window.purchasePageKeyHandler) {
		document.removeEventListener('keydown', window.purchasePageKeyHandler);
	}

	// Create new handler
	window.purchasePageKeyHandler = async (event) => {
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
	document.addEventListener('keydown', window.purchasePageKeyHandler);
};

// Product search functions for Sale and Order pages
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

// Material search functions for Purchase page
window.showMaterialSearchIndicator = (searchText) => {
	const indicator = document.getElementById('materialSearchIndicator');
	if (indicator) {
		indicator.style.display = 'block';
		document.getElementById('searchText').textContent = searchText;
	}
};

window.hideMaterialSearchIndicator = () => {
	const indicator = document.getElementById('materialSearchIndicator');
	if (indicator) {
		indicator.style.display = 'none';
	}
};

window.updateMaterialSearchIndicator = (searchText, resultCount) => {
	document.getElementById('searchText').textContent = searchText;
	document.getElementById('searchResults').textContent = `${resultCount} materials found`;
};