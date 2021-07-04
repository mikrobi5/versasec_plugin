// Copyright 2018 The Chromium Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

'use strict';

var port = null;
var hostName = "versasec.self_service_plugin";
var messageReceived = false;
var receviedMessage = "";

const portMap = new Map();
let portMessageId = 0;
/*
var sendMessageNative = function(message) {
  return new Promise(function(resolve, reject) {
	chrome.runtime.sendNativeMessage(hostName, message, function(response) {
            if (chrome.runtime.lastError) {
				reject(Error("ERROR: " + chrome.runtime.lastError.message));
            } else {
				console.log(response);
				resolve(response)
            }
        });
  });
}*/

var postMessageNative = function(message) {
	return new Promise(resolve => {
		const id = ++portMessageId;
		portMap.set(id, resolve);
		if(port)
		{
			console.log("send message number: ", id);
			port.postMessage({"id" : id, message}, "*");
		}
		else
		{
			resolve("{ 'error' : 'port is disconnected' }");
		}
	  });
  }

function onDisconnected() {
	console.log("Connection lost");
  port = null;
  
}

function sendPingMessage() {
	if(port)
	{
		console.log("Send PING message");
		(async () => {
		postMessageNative({"Command":"PING"}).then(function(pingResult) {
		if(pingResult)
		{
			console.log(pingResult);
			if(pingResult.control == 'PONG')
			{
				console.log("Pong recveived");
				setTimeout(function(){ sendPingMessage(); }, 3000);
			}
		}});
	})();
	}
	else
	{
		console.log("Ping-Pong connection lost");
	}
}

function sendEventMessage() {
	if(port)
	{
		console.log("Send Event request message");
		(async () => {
		postMessageNative({"Command":"Events"}).then(function(eventResult) {
		if(eventResult)
		{
			console.log(eventResult.Events);
			console.log(eventResult.Severity);
			if(eventResult.Message != '' )
			{
				if(eventResult.Severity == 'Positive')
				{
					positiveMessage(eventResult.Events)
				}
				if(eventResult.Severity == 'Negative')
				{
					negativeMessage(eventResult.Events)
				}
			}
			setTimeout(function(){ sendEventMessage(); }, 1000);
		}});
	})();
	}
	else
	{
		console.log("Ping-Pong connection lost");
	}
}

function positiveMessage(message) {

	var messageText = document.getElementById("sp_text");
		messageText.innerText = message;
	// Get the snackbar DIV
	var x = document.getElementById("snackbar_positive");
	// Add the "show" class to DIV
	x.className = "show";
  
	// After 3 seconds, remove the show class from DIV
	setTimeout(function(){ 
		x.className = x.className.replace("show", ""); 
		messageText.innerText = "";
	}, 1500);
  }

function negativeMessage(message) {
	var messageText = document.getElementById("sn_text");
	messageText.innerText = message;
	// Get the snackbar DIV
	var x = document.getElementById("snackbar_negative");
  
	// Add the "show" class to DIV
	x.className = "show";
  
	// After 3 seconds, remove the show class from DIV
	setTimeout(function(){ 
		x.className = x.className.replace("show", ""); 
		messageText.innerText = "";
	}, 1500);
  }

function start_native_communcation() {
  port = chrome.runtime.connectNative(hostName);
  if(port)   {
	port.onMessage.addListener(msg => {
		console.log("message recevied: ", msg);
		const {id, data} = msg;
		const resolve = portMap.get(id);
		portMap.delete(id);
		resolve(data);
	  });
	  port.onDisconnect.addListener(onDisconnected);
	  console.log("port is set");
	  setTimeout(function(){ sendPingMessage(); }, 3000);
	  setTimeout(function(){ sendEventMessage(); }, 1000);
  }
  else {
	  console.log("port is not set");
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
				/*	sendMessageNative({"Command":"RCCC"}).then(function(response) {
						sendResponse(response);
					});*/
              }
			  if(request.message == "RCCRC"){
				/*	sendMessageNative({"Command":"RCCRC"}).then(function(response) {
						sendResponse(response);
					});*/
              }
			  if(request.message == "LCC"){
				/*	sendMessageNative({"Command":"LCC"}).then(function(response) {
						sendResponse(response);
					});*/
              }
			  if(request.message == "RPCC"){
					/*sendMessageNative({"Command":"RPCC"}).then(function(response) {
						sendResponse(response);
					});*/
              }
			  if(request.message == "SNPCC"){
					/*sendMessageNative({"Command":"SNPCC"}).then(function(response) {
						sendResponse(response);
					});*/
              }
          }
      }
      return true;
  });

  //start_native_communcation();