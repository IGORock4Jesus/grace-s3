import React, { createContext, useContext, useState } from "react";
import type { GetMeResponse } from "~/generated/proxy/model";

interface AuthContextType {
	user: GetMeResponse | null;
	setUser: (user: GetMeResponse | null) => void;
}

const AuthContext = createContext<AuthContextType | null>(null);

export const AuthProvider = ({
	children,
	initialUser,
}: {
	children: React.ReactNode;
	initialUser: GetMeResponse | null;
}) => {
	const [user, setUser] = useState<GetMeResponse | null>(initialUser);
	return (
		<AuthContext.Provider value={{ user, setUser }}>
			{children}
		</AuthContext.Provider>
	);
};

export const useAuth = () => {
	const context = useContext(AuthContext);
	if (!context) throw new Error("useAuth must be used within AuthProvider");
	return context;
};
