/** eumm-template.js - (c) 2020 James Renwick
 *  -----------------------------------------
 *  ALL RIGHTS RESERVED. ABSOLUTELY NO WARRANTY.
*/

/**
 * Requests and returns the contents of the given file.
 * @param {string} path The path of the file to read.
 */
function loadFile(path, resolve, reject) {
    try {
        var req = new XMLHttpRequest();
        req.open("GET", path);
        req.onreadystatechange = function() {
            if(req.readyState === 4) {
                if(req.status === 200 || req.status == 0) {
                    resolve(req.responseText);
                } else {
                    reject();
                }
            }
        }
        req.onerror = reject;
        req.send();
    } catch (e) {
        reject();
        throw e;
    }
}

function onTemplateError() {
    alert("ERROR: Unable to load the webpage correctly! " +
          "Please report this at https://www.facebook.com/groups/edinburghmusicalmedics.");
}

function applyHeadTemplate(content) {
    let heads = document.getElementsByTagName('head');

    if (heads.length === 0) {
        const elem = document.createElement('head');
        document.appendChild(elem);
        heads = [elem];
    }
    heads[0].innerHTML += content;
}

function applyBodyTemplate(content) {
    let bodies = document.getElementsByTagName('body');

    if (bodies.length === 0) {
        const elem = document.createElement('body');
        document.appendChild(elem);
        bodies = [elem];
    }
    const body = bodies[0];

    // Convert the content string into nodes so that we can work with them
    const initialDiv = document.createElement('div');
    initialDiv.innerHTML = content;
    
    const nodesBySection = {};
    for (const elem of body.querySelectorAll('*[data-section]')) {
        const sectionName = elem.getAttribute('data-section');
        if (nodesBySection[sectionName] === undefined) {
            nodesBySection[sectionName] = [];
        }
        nodesBySection[sectionName].push(elem);
    }

    for (let elem of initialDiv.querySelectorAll('*[data-section]')) {
        const sectionName = elem.getAttribute('data-section');

        const div = elem;
        for (const node of (nodesBySection[sectionName] || [])) {
            elem.insertAdjacentElement('afterend', node);
            elem = node;
        }
        div.parentElement.removeChild(div);
    }

    const mainSection = initialDiv.querySelector('section[data-main-section]');
    for (const elem of Array.prototype.slice.call(body.children)) {
        if (!elem.getAttribute('data-section') && elem.id !== 'eumm-loading') {
            mainSection.appendChild(elem);
        }
    }

    body.innerHTML = initialDiv.innerHTML;
}

function applyDocumentTemplate() {
    // First hide the site
    const loadingDiv = document.getElementById('eumm-loading');
    loadingDiv.style.display = 'flex';

    // First load both _head and _body
    try {
        loadFile('_head.html', applyHeadTemplate, onTemplateError);
        loadFile('_body.html', applyBodyTemplate, onTemplateError);
    } catch (e) {
        onTemplateError();
        loadingDiv.style.display = 'none';
        throw e;
    }
}

applyDocumentTemplate();
