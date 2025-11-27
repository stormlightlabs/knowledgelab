import { getIconCollections, iconsPlugin } from "@egoist/tailwindcss-icons";
import typography from "@tailwindcss/typography";
/** @type import("tailwindcss").Config */
export default {
	content: ["./src/**/*.{fs,js,html}"],
	plugins: [
		typography(),
		iconsPlugin({
			collections: getIconCollections(["bi", "lucide", "mdi", "ri", "noto"]),
		}),
	],
};
