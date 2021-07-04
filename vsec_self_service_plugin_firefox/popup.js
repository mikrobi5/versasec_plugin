// Copyright 2018 The Chromium Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

'use strict';

//var hostName = "com.google.chrome.example.echo";
//import { sendMessageNative } from './commons.js';

// var sendMessageNative = function(message) {
  // return new Promise(function(resolve, reject) {
    
	// chrome.runtime.sendNativeMessage(hostName, message, function(response) {
            // if (chrome.runtime.lastError) {
                // //alert("ERROR: " + chrome.runtime.lastError.message);
				// reject(Error("ERROR: " + chrome.runtime.lastError.message));
            // } else {
				// console.log(response);
                // //var resp = JSON.parse(response);
				// //console.log(resp);
				// //onNativeMessage(response);
				// resolve(response)
            // }
        // });
  // });
// }

document.addEventListener('DOMContentLoaded', function () {
		//document.getElementById('connect-button').addEventListener(
		//  'click', openPipe);
		  //RCCC request current card csn
		//  var port = browser.runtime.connectNative(hostName);
		  
		document.getElementById('send-message-get-card-id').addEventListener(
		  'click', function() { 
		    sendMessageNative({"Command":"RCCC"}).then(function(response) {
			 document.getElementById("cardId").value = JSON.stringify(response);
			});
		  });
		  //RCCRC request current card request counter
		document.getElementById('send-message-get-retry-counter').addEventListener(
		  'click', function() { 
			sendMessageNative({ "Command": "RCCRC"}).then(function(result){
			 document.getElementById("retryCounter").value = JSON.stringify(result);
			});
		  });
		  //lcc login current card
		document.getElementById('send-message-login').addEventListener(
		  'click', function() { 
			sendMessageNative({ "Command": "LCC"}).then(function(result){
			 document.getElementById("login").value = JSON.stringify(result);
			});
		  });
		  //RPCC reset pin current card
		document.getElementById('send-message-reset-pin').addEventListener(
		  'click', function() { 
			sendMessageNative({ "Command": "RPCC"}).then(function(result){
			 document.getElementById("resetPin").value = JSON.stringify(result);
			});
		  });
		  //SNPCC set new pin current card
		document.getElementById('send-message-set-new-pin').addEventListener(
		  'click', function() { 
			sendMessageNative({ "Command": "SNPCC"}).then(function(result){
			 document.getElementById("oldPin").value = JSON.stringify(result);
			 document.getElementById("newPin").value = JSON.stringify(result);
			});
		  });
		 // document.getElementById('send-message-button').addEventListener(
		 // 'click', sendNativeMessage);
  
	});