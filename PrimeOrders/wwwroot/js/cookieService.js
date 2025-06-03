function setCookie(name, value, hours) {
	const date = new Date();
	date.setTime(date.getTime() + (hours * 60 * 60 * 1000)); // Convert hours to milliseconds
	const expires = "expires=" + date.toUTCString();
	document.cookie = name + "=" + value + ";" + expires + ";path=/";
}

function getCookie(name) {
	const cookieName = name + "=";
	const decodedCookie = decodeURIComponent(document.cookie);
	const cookies = decodedCookie.split(';');
	for (let i = 0; i < cookies.length; i++) {
		let cookie = cookies[i].trim();
		if (cookie.startsWith(cookieName)) {
			return cookie.substring(cookieName.length, cookie.length);
		}
	}
	return "";
}

function deleteCookie(name) {
	// Set cookie with past expiration date (standard way)
	document.cookie = name + '=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT';

	// Also try without domain specification
	document.cookie = name + '=; path=/; max-age=-99999999;';

	// If needed, also try with domain
	if (location.hostname.indexOf('.') !== -1) {
		// For domains with dots (e.g. example.com)
		const domain = location.hostname.split('.').slice(-2).join('.');
		document.cookie = name + '=; path=/; domain=.' + domain + '; expires=Thu, 01 Jan 1970 00:00:00 GMT';
	}
}