// Copyright 2018 The Chromium Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

'use strict';

let page = document.getElementById('buttonDiv');

function constructOptions() {
  
    let button = document.createElement('button');
    button.style.backgroundColor = '#f9482d';
	button.textContent = "Please Set";
    button.addEventListener('click', function() {
         alert("here can happen anything");
    });
    page.appendChild(button);
  
}
constructOptions();
