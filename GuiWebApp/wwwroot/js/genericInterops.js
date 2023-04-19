
async function downloadFileFromStream(fileName, contentStreamReference) {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
}

function simulateClick(id) {
    let el = document.getElementById(id);
    if (el != null) {
        el.click();
    }
}

function selectFlightList(e, v){
    let elem = document.getElementById(e);
    if(elem !== null && elem !== undefined){
        elem.value = v;
    }
}