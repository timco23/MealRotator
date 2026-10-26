// Standard Blazor file download: stream the bytes to the browser and click a temporary link.
export async function downloadFile(fileName, streamRef) {
  const blob = await (await streamRef).blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}
