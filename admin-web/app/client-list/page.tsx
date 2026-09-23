import { withAuthLoader } from "~/lib/auth-loader";
import { useState } from "react";
import { useRevalidator } from "react-router";
import { isAxiosError } from "axios";
import type { Route } from "./+types/page";
import { createClient, getClientList } from "~/generated/api/client/clients";
import { customInstance } from "~/lib/http-client";
import { Button } from "~/components/ui/button";
import { Input } from "~/components/ui/input";
import { Label } from "~/components/ui/label";

export function meta() { return [{ title: "Clients" }]; }
export const clientLoader = withAuthLoader(async ({ request }: Route.ClientLoaderArgs) => {
	return getClientList({ Skip: 0, Take: 10 }, request.signal);
});
export default function ClientListPage({ loaderData }: Route.ComponentProps) {
	const revalidator = useRevalidator();
	const [busy, setBusy] = useState(false);
	const [error, setError] = useState("");
	async function add(event: React.FormEvent<HTMLFormElement>) {
		event.preventDefault();
		const form = event.currentTarget;
		const data = new FormData(form);
		setBusy(true); setError("");
		try {
			await createClient({ accessKey: String(data.get("accessKey")).trim(), secretKey: String(data.get("secretKey")) });
			form.reset(); await revalidator.revalidate();
		} catch (error) {
			setError(isAxiosError(error) && error.response?.status === 409 ? "This access key already exists." : "Could not create client. Check the fields and try again.");
		} finally { setBusy(false); }
	}
	async function remove(id: string, accessKey: string) {
		if (!window.confirm(`Delete client “${accessKey}”? Clients with files cannot be deleted.`)) return;
		setBusy(true); setError("");
		try {
			await customInstance({ url: `/api/clients/${id}`, method: "DELETE" });
			await revalidator.revalidate();
		} catch (error) {
			setError(isAxiosError(error) && error.response?.status === 409 ? "This client has files and cannot be deleted." : "Could not delete client. Try again.");
		} finally { setBusy(false); }
	}
	return <div className="container mx-auto space-y-6 p-6">
		<h1 className="text-2xl font-semibold">Clients</h1>
		<form onSubmit={add} className="flex flex-wrap items-end gap-4 rounded-md border p-4">
			<div className="space-y-2"><Label htmlFor="accessKey">Access key</Label><Input id="accessKey" name="accessKey" required maxLength={100} disabled={busy} /></div>
			<div className="space-y-2"><Label htmlFor="secretKey">Secret key</Label><Input id="secretKey" name="secretKey" type="password" autoComplete="new-password" required disabled={busy} /></div>
			<Button disabled={busy} type="submit">Add client</Button>
		</form>
		{error && <p role="alert" className="text-destructive">{error}</p>}
		<div className="overflow-x-auto rounded-md border"><table className="w-full text-left text-sm">
			<thead><tr className="border-b"><th className="p-3">Access key</th><th className="p-3">ID</th><th className="p-3">Actions</th></tr></thead>
			<tbody>{loaderData.map(client => <tr key={client.id} className="border-b"><td className="p-3">{client.accessKey}</td><td className="p-3">{client.id}</td><td className="p-3"><Button variant="destructive" disabled={busy} onClick={() => remove(client.id, client.accessKey)}>Delete</Button></td></tr>)}</tbody>
		</table>{!loaderData.length && <p className="p-6 text-center">No clients yet.</p>}</div>
	</div>;
}
