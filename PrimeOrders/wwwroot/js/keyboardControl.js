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

window.setupKitchenIssuePageKeyboardHandlers = (dotNetHelper) => {
	// Remove existing listeners first
	if (window.kitchenIssuePageKeyHandler) {
		document.removeEventListener('keydown', window.kitchenIssuePageKeyHandler);
	}

	// Create new handler
	window.kitchenIssuePageKeyHandler = async (event) => {
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
	document.addEventListener('keydown', window.kitchenIssuePageKeyHandler);
};

window.setupKitchenProductionPageKeyboardHandlers = (dotNetHelper) => {
	// Remove existing listeners first
	if (window.kitchenProductionPageKeyHandler) {
		document.removeEventListener('keydown', window.kitchenProductionPageKeyHandler);
	}

	// Create new handler
	window.kitchenProductionPageKeyHandler = async (event) => {
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
	document.addEventListener('keydown', window.kitchenProductionPageKeyHandler);
};

window.setupStockAdjustmentPageKeyboardHandlers = (dotNetHelper) => {
	// Remove existing listeners first
	if (window.stockAdjustmentPageKeyHandler) {
		document.removeEventListener('keydown', window.stockAdjustmentPageKeyHandler);
	}

	// Create new handler
	window.stockAdjustmentPageKeyHandler = async (event) => {
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
	document.addEventListener('keydown', window.stockAdjustmentPageKeyHandler);
};

window.setupProductStockAdjustmentPageKeyboardHandlers = (dotNetHelper) => {
	// Remove existing listeners first
	if (window.productStockAdjustmentPageKeyHandler) {
		document.removeEventListener('keydown', window.productStockAdjustmentPageKeyHandler);
	}

	// Create new handler
	window.productStockAdjustmentPageKeyHandler = async (event) => {
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
	document.addEventListener('keydown', window.productStockAdjustmentPageKeyHandler);
};

// Product search functions for Sale, Order, Kitchen Production, and Product Stock Adjustment pages
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

// Material search functions for Purchase, Kitchen Issue, and Raw Material Stock Adjustment pages
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