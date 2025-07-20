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

window.setupSaleReturnPageKeyboardHandlers = (dotNetHelper) => {
	// Remove existing listeners first
	if (window.saleReturnPageKeyHandler) {
		document.removeEventListener('keydown', window.saleReturnPageKeyHandler);
	}

	// Create new handler
	window.saleReturnPageKeyHandler = async (event) => {
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
	document.addEventListener('keydown', window.saleReturnPageKeyHandler);
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

// Product search functions for Sale, Sale Return, Order, Kitchen Production, and Product Stock Adjustment pages
window.showProductSearchIndicator = (searchText) => {
	const indicator = document.getElementById('productSearchIndicator');
	if (indicator) {
		indicator.style.display = 'block';
		const searchTextElement = document.getElementById('searchText');
		if (searchTextElement) {
			searchTextElement.textContent = searchText;
		}
	}
};

window.hideProductSearchIndicator = () => {
	const indicator = document.getElementById('productSearchIndicator');
	if (indicator) {
		indicator.style.display = 'none';
	}
};

window.updateProductSearchIndicator = (searchText, resultCount) => {
	const searchTextElement = document.getElementById('searchText');
	const searchResultsElement = document.getElementById('searchResults');

	if (searchTextElement) {
		searchTextElement.textContent = searchText;
	}
	if (searchResultsElement) {
		searchResultsElement.textContent = `${resultCount} products found`;
	}
};

// Material search functions for Purchase, Kitchen Issue, and Raw Material Stock Adjustment pages
window.showMaterialSearchIndicator = (searchText) => {
	const indicator = document.getElementById('materialSearchIndicator');
	if (indicator) {
		indicator.style.display = 'block';
		const searchTextElement = document.getElementById('searchText');
		if (searchTextElement) {
			searchTextElement.textContent = searchText;
		}
	}
};

window.hideMaterialSearchIndicator = () => {
	const indicator = document.getElementById('materialSearchIndicator');
	if (indicator) {
		indicator.style.display = 'none';
	}
};

window.updateMaterialSearchIndicator = (searchText, resultCount) => {
	const searchTextElement = document.getElementById('searchText');
	const searchResultsElement = document.getElementById('searchResults');

	if (searchTextElement) {
		searchTextElement.textContent = searchText;
	}
	if (searchResultsElement) {
		searchResultsElement.textContent = `${resultCount} materials found`;
	}
};

// Cleanup function to remove all event listeners when needed
window.cleanupKeyboardHandlers = () => {
	if (window.salePageKeyHandler) {
		document.removeEventListener('keydown', window.salePageKeyHandler);
		window.salePageKeyHandler = null;
	}
	if (window.saleReturnPageKeyHandler) {
		document.removeEventListener('keydown', window.saleReturnPageKeyHandler);
		window.saleReturnPageKeyHandler = null;
	}
	if (window.orderPageKeyHandler) {
		document.removeEventListener('keydown', window.orderPageKeyHandler);
		window.orderPageKeyHandler = null;
	}
	if (window.purchasePageKeyHandler) {
		document.removeEventListener('keydown', window.purchasePageKeyHandler);
		window.purchasePageKeyHandler = null;
	}
	if (window.kitchenIssuePageKeyHandler) {
		document.removeEventListener('keydown', window.kitchenIssuePageKeyHandler);
		window.kitchenIssuePageKeyHandler = null;
	}
	if (window.kitchenProductionPageKeyHandler) {
		document.removeEventListener('keydown', window.kitchenProductionPageKeyHandler);
		window.kitchenProductionPageKeyHandler = null;
	}
	if (window.stockAdjustmentPageKeyHandler) {
		document.removeEventListener('keydown', window.stockAdjustmentPageKeyHandler);
		window.stockAdjustmentPageKeyHandler = null;
	}
	if (window.productStockAdjustmentPageKeyHandler) {
		document.removeEventListener('keydown', window.productStockAdjustmentPageKeyHandler);
		window.productStockAdjustmentPageKeyHandler = null;
	}
};

// Additional utility functions that might be needed for Blazor integration
window.focusElement = (elementId) => {
	const element = document.getElementById(elementId);
	if (element) {
		element.focus();
	}
};

window.scrollToElement = (elementId) => {
	const element = document.getElementById(elementId);
	if (element) {
		element.scrollIntoView({ behavior: 'smooth' });
	}
};

window.setupAccountingPageKeyboardHandlers = (dotNetHelper) => {
	// Remove existing listeners first
	if (window.accountingPageKeyHandler) {
		document.removeEventListener('keydown', window.accountingPageKeyHandler);
	}

	// Create new handler
	window.accountingPageKeyHandler = async (event) => {
		// Don't handle keys when typing in input fields
		if (['INPUT', 'TEXTAREA', 'SELECT'].includes(event.target.tagName)) {
			return;
		}

		try {
			await dotNetHelper.invokeMethodAsync('HandleKeyboardShortcut', event.key);
		} catch (error) {
			console.error('Keyboard handler error:', error);
		}

		// Prevent default for function keys
		if (event.key.startsWith('F') || event.ctrlKey) {
			event.preventDefault();
		}
	};

	// Add new listener
	document.addEventListener('keydown', window.accountingPageKeyHandler);
};

// Ledger search functions for Financial Accounting page
window.showLedgerSearchIndicator = (searchText) => {
	const indicator = document.getElementById('ledgerSearchIndicator');
	if (indicator) {
		indicator.style.display = 'block';
		const searchTextElement = document.getElementById('searchText');
		if (searchTextElement) {
			searchTextElement.textContent = searchText;
		}
	}
};

window.hideLedgerSearchIndicator = () => {
	const indicator = document.getElementById('ledgerSearchIndicator');
	if (indicator) {
		indicator.style.display = 'none';
	}
};

window.updateLedgerSearchIndicator = (searchText, resultCount) => {
	const searchTextElement = document.getElementById('searchText');
	const searchResultsElement = document.getElementById('searchResults');

	if (searchTextElement) {
		searchTextElement.textContent = searchText;
	}
	if (searchResultsElement) {
		searchResultsElement.textContent = `${resultCount} ledgers found`;
	}
};

// Update the cleanup function to include accounting page handler
window.cleanupKeyboardHandlers = () => {
	if (window.salePageKeyHandler) {
		document.removeEventListener('keydown', window.salePageKeyHandler);
		window.salePageKeyHandler = null;
	}
	if (window.saleReturnPageKeyHandler) {
		document.removeEventListener('keydown', window.saleReturnPageKeyHandler);
		window.saleReturnPageKeyHandler = null;
	}
	if (window.orderPageKeyHandler) {
		document.removeEventListener('keydown', window.orderPageKeyHandler);
		window.orderPageKeyHandler = null;
	}
	if (window.purchasePageKeyHandler) {
		document.removeEventListener('keydown', window.purchasePageKeyHandler);
		window.purchasePageKeyHandler = null;
	}
	if (window.kitchenIssuePageKeyHandler) {
		document.removeEventListener('keydown', window.kitchenIssuePageKeyHandler);
		window.kitchenIssuePageKeyHandler = null;
	}
	if (window.kitchenProductionPageKeyHandler) {
		document.removeEventListener('keydown', window.kitchenProductionPageKeyHandler);
		window.kitchenProductionPageKeyHandler = null;
	}
	if (window.stockAdjustmentPageKeyHandler) {
		document.removeEventListener('keydown', window.stockAdjustmentPageKeyHandler);
		window.stockAdjustmentPageKeyHandler = null;
	}
	if (window.productStockAdjustmentPageKeyHandler) {
		document.removeEventListener('keydown', window.productStockAdjustmentPageKeyHandler);
		window.productStockAdjustmentPageKeyHandler = null;
	}
	if (window.accountingPageKeyHandler) {
		document.removeEventListener('keydown', window.accountingPageKeyHandler);
		window.accountingPageKeyHandler = null;
	}
};