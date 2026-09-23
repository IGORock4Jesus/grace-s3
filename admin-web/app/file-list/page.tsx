import { withAuthLoader } from "~/lib/auth-loader";
import { useState } from "react";
import { isAxiosError } from "axios";
import { useQuery } from "@tanstack/react-query";
import { FolderIcon } from "lucide-react";
import type { Route } from "./+types/page";
import { getClientList } from "~/generated/api/client/clients";
import { customInstance } from "~/lib/http-client";
import { Button } from "~/components/ui/button";

type StoredFile = { id: string; fileName: string; contentType: string; size: number };
export function meta() { return [{ title: "Files" }]; }
export const clientLoader = withAuthLoader(async ({ request }: Route.ClientLoaderArgs) => {
	return getClientList({ Skip: 0, Take: 10 }, request.signal);
});
function ClientFolder({ id, accessKey }: { id: string; accessKey: string }) {
	const [open, setOpen] = useState(false);
	const [deleting, setDeleting] = useState<string | null>(null);
	const [deleteError, setDeleteError] = useState("");
	const files = useQuery({ queryKey: ["client-files", id], enabled: open,
		queryFn: ({ signal }) => customInstance<StoredFile[]>({ url: `/api/clients/${id}/files`, method: "GET", signal }) });
	async function deleteFile(file: StoredFile) {
		if (deleting || !window.confirm(`Delete “${file.fileName}” from client “${accessKey}”? This cannot be undone.`)) return;
		setDeleting(file.id);
		setDeleteError("");
		try {
			await customInstance({ url: `/api/clients/${id}/files/${file.id}`, method: "DELETE" });
			await files.refetch();
		} catch (error) {
			if (isAxiosError(error) && error.response?.status === 404) {
				await files.refetch();
			} else {
				setDeleteError("Could not delete the file. Try again.");
			}
		} finally { setDeleting(null); }
	}
	return <section className="rounded-md border">
		<button type="button" aria-expanded={open} aria-controls={`files-${id}`} className="flex w-full items-center gap-3 p-4 text-left font-medium hover:bg-muted" onClick={() => setOpen(!open)}><FolderIcon className="size-5" />{accessKey}<span className="ml-auto">{open ? "−" : "+"}</span></button>
		{open && <div id={`files-${id}`} className="overflow-x-auto border-t p-4">
			{deleteError && <p role="alert" className="mb-3 text-destructive">{deleteError}</p>}
			{files.isPending ? <p role="status">Loading files…</p> : files.isError ? <div role="alert">Could not load files. <Button variant="outline" onClick={() => files.refetch()}>Retry</Button></div> : !files.data.length ? <p>No files in this folder.</p> :
			<table className="w-full text-left text-sm"><thead><tr><th className="p-2">Name ↑</th><th className="p-2">Type</th><th className="p-2">Size</th><th className="p-2">Actions</th></tr></thead><tbody>
				{files.data.map(file => <tr key={file.id} className="border-t"><td className="break-all p-2">{file.fileName}</td><td className="p-2">{file.contentType}</td><td className="whitespace-nowrap p-2">{file.size.toLocaleString()} B</td><td className="p-2"><a className="underline" href={`/api/clients/${id}/files/${file.id}`} download={file.fileName}>Download<span className="sr-only"> {file.fileName}</span></a><Button type="button" variant="destructive" size="sm" className="ml-3" disabled={deleting !== null} onClick={() => deleteFile(file)}>{deleting === file.id ? "Deleting…" : "Delete"}<span className="sr-only"> {file.fileName}</span></Button></td></tr>)}
			</tbody></table>}
		</div>}
	</section>;
}
export default function FileListPage({ loaderData }: Route.ComponentProps) {
	return <div className="container mx-auto space-y-4 p-6"><h1 className="text-2xl font-semibold">Files</h1><p className="text-muted-foreground">Folders are named by client access key. Folders and files are sorted by name.</p>
		{loaderData.length ? loaderData.map(client => <ClientFolder key={client.id} id={client.id} accessKey={client.accessKey} />) : <p>No client folders yet.</p>}
	</div>;
}
