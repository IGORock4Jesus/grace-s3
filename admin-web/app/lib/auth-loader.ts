import { isAxiosError } from "axios";
import { redirectDocument } from "react-router";

// Parent and child loaders run concurrently. Each loader must translate its own
// unauthorized response into a document navigation to the BFF's OIDC endpoint.
export function withAuthLoader<Args extends { request: Request }, Result>(
	loader: (args: Args) => Promise<Result>,
): (args: Args) => Promise<Result> {
	return async (args) => {
		try {
			return await loader(args);
		} catch (error) {
			if (isAxiosError(error) && error.response?.status === 401) {
				const url = new URL(args.request.url);
				const returnUrl = url.pathname + url.search + url.hash;
				throw redirectDocument(`/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`);
			}
			throw error;
		}
	};
}
