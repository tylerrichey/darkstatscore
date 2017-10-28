var csv = require("fast-csv");
var copyfiles = require('copyfiles');
 
csv
 .fromPath("copypackages.csv")
 .on("data", function(data){
	 copyfiles(data, [], function(){});
 })
 .on("end", function(){
     console.log("Copy packages completed.");
 });