import { defineConfig } from "orval";

export default defineConfig({
	proxy: {
		output: {
			mode: "tags",
			target: "app/generated/proxy/client",
			schemas: "app/generated/proxy/model",
			client: "react-query",
			httpClient: "axios",
			clean: true,
			baseUrl: "",
			override: {
				mutator: {
					path: "./app/lib/http-client.ts",
					name: "customInstance",
				},
			},
		},
		input: {
			target: "http://localhost:4000/openapi/v1.json",
		},
		hooks: {
			afterAllFilesWrite: "prettier --write",
		},
	},
	admin: {
		output: {
			mode: "tags",
			target: "app/generated/api/client",
			schemas: "app/generated/api/model",
			client: "react-query",
			httpClient: "axios",
			clean: true,
			baseUrl: "/api",
			override: {
				mutator: {
					path: "./app/lib/http-client.ts",
					name: "customInstance",
				},
			},
		},
		input: {
			target: "http://localhost:5000/openapi/v1.json",
		},
		hooks: {
			afterAllFilesWrite: "prettier --write",
		},
	},
});
