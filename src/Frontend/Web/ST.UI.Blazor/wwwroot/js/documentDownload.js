export async function download(fileName, contentType, reference) {
    const content = await reference.arrayBuffer();
    const url = URL.createObjectURL(new Blob([content], { type: contentType }));
    const link = document.createElement("a");

    try {
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
    } finally {
        link.remove();
        URL.revokeObjectURL(url);
    }
}
