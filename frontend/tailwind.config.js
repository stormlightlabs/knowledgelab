import { getIconCollections, iconsPlugin } from "@egoist/tailwindcss-icons";
import forms from "@tailwindcss/forms";
import typography from "@tailwindcss/typography";

/** @type import("tailwindcss").Config */
export default {
  content: ["./src/**/*.{fs,js,html}"],
  plugins: [
    typography(),
    forms(),
    iconsPlugin({
      collections: getIconCollections(["bi", "lucide", "mdi", "ri", "noto"]),
    }),
  ],
};
