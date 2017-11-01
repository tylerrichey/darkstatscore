var copy = require("copy");
var packages = require("./copypackages.files.json");

packages.forEach(function (tocopy) {
	copy(tocopy[0], tocopy[1], function(err, files) {
		if (err) throw err;
		files.forEach(function (copied) {
			console.log("Successfully copied: " + copied.path);
		});
	});
});
