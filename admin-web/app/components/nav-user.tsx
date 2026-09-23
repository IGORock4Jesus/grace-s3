import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuGroup,
	DropdownMenuItem,
	DropdownMenuLabel,
	DropdownMenuSeparator,
	DropdownMenuTrigger,
} from "~/components/ui/dropdown-menu";
import {
	SidebarMenu,
	SidebarMenuButton,
	SidebarMenuItem,
	useSidebar,
} from "~/components/ui/sidebar";
import {
	EllipsisVerticalIcon,
	CircleUserRoundIcon,
	CreditCardIcon,
	BellIcon,
	LogOutIcon,
} from "lucide-react";
import { useAuth } from "~/lib/auth-context";
import { useNavigate } from "react-router";

export function NavUser() {
	const { isMobile } = useSidebar();

	const auth = useAuth();
	const navigate = useNavigate();

	function logOut() {
		navigate("/auth/logout");
	}

	return (
		<SidebarMenu>
			<SidebarMenuItem>
				<DropdownMenu>
					<DropdownMenuTrigger
						render={
							<SidebarMenuButton
								size="lg"
								className="aria-expanded:bg-muted"
							/>
						}
					>
						<Avatar className="size-8 rounded-lg grayscale">
							{/* <AvatarImage src={user.avatar} alt={user.name} /> */}
							<AvatarFallback className="rounded-lg">
								CN
							</AvatarFallback>
						</Avatar>
						<div className="grid flex-1 text-left text-sm leading-tight">
							<span className="truncate font-medium">
								{auth.user?.username}
							</span>
							<span className="truncate text-xs text-foreground/70">
								{auth.user?.email}
							</span>
						</div>
						<EllipsisVerticalIcon className="ml-auto size-4" />
					</DropdownMenuTrigger>
					<DropdownMenuContent
						className="min-w-56"
						side={isMobile ? "bottom" : "right"}
						align="end"
						sideOffset={4}
					>
						<DropdownMenuGroup>
							<DropdownMenuLabel className="p-0 font-normal">
								<div className="flex items-center gap-2 px-1 py-1.5 text-left text-sm">
									<Avatar className="size-8">
										{/* <AvatarImage
											src={user.avatar}
											alt={user.name}
										/> */}
										<AvatarFallback className="rounded-lg">
											CN
										</AvatarFallback>
									</Avatar>
									<div className="grid flex-1 text-left text-sm leading-tight">
										<span className="truncate font-medium">
											{auth.user?.username}
										</span>
										<span className="truncate text-xs text-muted-foreground">
											{auth.user?.email}
										</span>
									</div>
								</div>
							</DropdownMenuLabel>
						</DropdownMenuGroup>
						<DropdownMenuSeparator />
						<DropdownMenuGroup>
							<DropdownMenuItem>
								<CircleUserRoundIcon />
								Account
							</DropdownMenuItem>
							<DropdownMenuItem>
								<CreditCardIcon />
								Billing
							</DropdownMenuItem>
							<DropdownMenuItem>
								<BellIcon />
								Notifications
							</DropdownMenuItem>
						</DropdownMenuGroup>
						<DropdownMenuSeparator />
						<DropdownMenuItem onClick={logOut}>
							<LogOutIcon />
							Log out
						</DropdownMenuItem>
					</DropdownMenuContent>
				</DropdownMenu>
			</SidebarMenuItem>
		</SidebarMenu>
	);
}
