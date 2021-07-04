// Copyright 2018 The Chromium Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

'use strict';

var port = null;
var hostName = "versasec_self_service_plugin";
var browser = browser || chrome;


if(chrome)
{
	var sendMessageNative = function(message) {
  return new Promise(function(resolve, reject) {
    
	chrome.runtime.sendNativeMessage(hostName, message, function(response) {
            if (chrome.runtime.lastError) {
                //alert("ERROR: " + chrome.runtime.lastError.message);
				reject(Error("ERROR: " + chrome.runtime.lastError.message));
            } else {
				console.log(response);
                //var resp = JSON.parse(response);
				//console.log(resp);
				//onNativeMessage(response);
				resolve(response)
            }
        });
  });
}
}
else
{
	var sendMessageNative = function(message) {
	console.log("Sending: ");
	console.log(message);
  var sending = browser.runtime.sendNativeMessage(
    hostName,
    message);
  return sending; //.then(onResponse, onError);
 }
}
chrome.runtime.onMessageExternal.addListener(
  function(request, sender, sendResponse) {
      if (request) {
          if (request.message) {
              if (request.message == "version") {
                  sendResponse({version: 1.0});
              }
              if(request.message == "pipe_com"){
                sendResponse({message: "pipe com"});
				if(!port)
				{
					start_native_communcation();
				}
				else{
					console.log("already connected")
				}	
              }
			  if(request.message == "RCCC"){
					sendMessageNative({"Command":"RCCC"}).then(function(response) {
						sendResponse(response);
					});
              }
			  if(request.message == "RCCRC"){
					sendMessageNative({"Command":"RCCRC"}).then(function(response) {
						sendResponse(response);
					});
              }
			  if(request.message == "LCC"){
					sendMessageNative({"Command":"LCC"}).then(function(response) {
						sendResponse(response);
					});
              }
			  if(request.message == "RPCC"){
					sendMessageNative({"Command":"RPCC"}).then(function(response) {
						sendResponse(response);
					});
              }
			  if(request.message == "SNPCC"){
					sendMessageNative({"Command":"SNPCC"}).then(function(response) {
						sendResponse(response);
					});
              }
          }
      }
      return true;
  });
