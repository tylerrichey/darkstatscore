var copyfiles = require("copyfiles");
var packages = require("./copypackages.files.json");

packages.forEach(function (files) {
	copyfiles(files, [], function(){});
	console.log("Copied " + files[0] + " to " + files[1]);
});

console.log("Copy packages to wwwroot completed.");
console.log();