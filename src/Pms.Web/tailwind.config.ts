import type { Config } from "tailwindcss";

export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      fontFamily: {
        sans: ["IBM Plex Sans", "ui-sans-serif", "system-ui"],
        mono: ["JetBrains Mono", "ui-monospace", "SFMono-Regular"]
      },
      colors: {
        ink: "#111318",
        mist: "#f2f4f8",
        tide: "#164e63",
        tideSoft: "#a5d8e6",
        ember: "#f97316",
        emberSoft: "#fed7aa",
        moss: "#14532d",
        slateDark: "#1f2937"
      },
      boxShadow: {
        panel: "0 20px 60px rgba(15, 23, 42, 0.15)",
        soft: "0 8px 24px rgba(15, 23, 42, 0.08)"
      }
    }
  },
  plugins: []
} satisfies Config;
