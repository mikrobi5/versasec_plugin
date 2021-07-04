// Copyright 2018 The Chromium Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

'use strict';

  
  // Close the dropdown if the user clicks outside of it
  window.onclick = function(e) {
	if (!e.target.matches('.dropbtn')) {
	var myDropdown = document.getElementById("readers_dropdown");
	  if (myDropdown.classList.contains('show')) {
		myDropdown.classList.remove('show');
	  }
	}
  }

document.addEventListener('DOMContentLoaded', function () {
	    document.getElementById('send_message_get_card_readers').addEventListener(
		'click', function() { 
		 (async () => {
		    postMessageNative({"Command":"GCRS"}).then(function(response) {
		        //var test = response.GCRS;
				response.GCRS.forEach(function(item, index) {
					var test = document.createElement("div");
					test.innerText = item.ReaderName + " - Card: " + item.Index;
					test.addEventListener(
						'click', function() { 
							document.getElementById("card_functions").classList.toggle("show");
							document.getElementById("card_functions").setAttribute("value", item.Csn);
						});
					//"<a href=\"#\" value=\"" + item.CSN + "\">" + item.ReaderName + " - Card: " + item.Index + "</a>"
				    document.getElementById("readers_dropdown").appendChild(test);
				});
				document.getElementById("dropdown_readers_main").classList.toggle("show");
		    });
		   })();
		});

		document.getElementById('dropdown_readers_button').addEventListener(
			'click', function() { 
				document.getElementById("readers_dropdown").classList.toggle("show");
			});

		document.getElementById('send-message-get-card-id').addEventListener(
		   'click', function() { 
		   (async () => {
             postMessageNative({"Command":"RCCC", "Values" : { "CardCsn" : document.getElementById("card_functions").getAttribute("value") }}).then(function(response) {
		 	 document.getElementById("cardId").value = response.CSN;
		 	 });
		   })();
		});   
		//RCCRC request current card request counter
		 document.getElementById('send-message-get-retry-counter').addEventListener(
		   'click', function() { 
		 	(async () => {
		 		postMessageNative({"Command":"RCCRC", "Values" : { "CardCsn" : document.getElementById("card_functions").getAttribute("value") }}).then(function(response) {
		 		document.getElementById("retryCounter").value = response.RCCRC;
		 		});
		 	   })();
		   });
		//lcc login current card
		document.getElementById('send-message-login').addEventListener(
		   'click', function() { 
		 	(async () => {
		 		var password = document.getElementById("login").value;
		 		postMessageNative({"Command":"LCC", "Values" : { "CardCsn" : document.getElementById("card_functions").getAttribute("value") ,"PinOne" : password }}).then(function(response) {
		 		document.getElementById("login").value = response.LCC;
		 		});
		 	   })();
		 	// sendMessageNative({ "Command": "LCC"}).then(function(result){
		 	//  document.getElementById("login").value = JSON.stringify(result);
		 	// });
		   });
		//RPCC reset pin current card
		document.getElementById('send-message-reset-pin').addEventListener(
		   'click', function() { 
		 	(async () => {
		 		var oldPin = document.getElementById("resetPin").value;
		 		postMessageNative({"Command":"RPCC", "Values" : { "CardCsn" : document.getElementById("card_functions").getAttribute("value") ,"PinOne" : oldPin }}).then(function(response) {
		 		document.getElementById("resetPin").value = response.RPCC;
		 		});
		 	   })();
		 	// sendMessageNative({ "Command": "RPCC"}).then(function(result){
		 	//  document.getElementById("resetPin").value = JSON.stringify(result);
		 	// });
		   });
		//SNPCC set new pin current card
		 document.getElementById('send-message-set-new-pin').addEventListener(
		   'click', function() { 
		 	(async () => {
		 		var oldPin = document.getElementById("oldPin").value;
		 		var newPin = document.getElementById("newPin").value;
		 			postMessageNative({"Command":"SNPCC", "Values" : { "CardCsn" : document.getElementById("card_functions").getAttribute("value") ,"PinOne" : oldPin, "PinTwo" : newPin }}).then(function(response) {
		 			document.getElementById("oldPin").value = response.SNPCC;
		 		});
		 	   })();
		 	// sendMessageNative({ "Command": "SNPCC"}).then(function(result){
		 	//  document.getElementById("oldPin").value = JSON.stringify(result);
		 	//  document.getElementById("newPin").value = JSON.stringify(result);
		 	// });
		});
		 // document.getElementById('send-message-button').addEventListener(
		 // 'click', sendNativeMessage);
		start_native_communcation();
	});