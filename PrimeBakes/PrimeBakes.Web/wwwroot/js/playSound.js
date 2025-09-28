window.PlaySound = function (soundFileName) {
	const fileName = soundFileName || "checkout.mp3";
	const audio = new Audio(`sound/${fileName}`);
	audio.play().catch(error => {
		console.error(`Error playing sound ${fileName}:`, error);
	});
};