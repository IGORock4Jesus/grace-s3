import {
	type RouteConfig,
	index,
	layout,
	route,
} from "@react-router/dev/routes";

export default [
	layout("routes/layout.tsx", [
		index("routes/home.tsx"),
		route("/clients", "client-list/page.tsx"),
		route("/files", "file-list/page.tsx"),
		// route("login", "./auth/login.tsx"),
		// route("register", "./auth/register.tsx"),
	]),

	// route("dashboard", "routes/dashboard.tsx"),
] satisfies RouteConfig;
