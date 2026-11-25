import { cpSync, mkdirSync } from "node:fs";
import { resolve } from "node:path";
import { defineConfig } from "vite";

export default defineConfig({
    base: "./",
    build: {
        outDir: "../Web/dist",
        emptyOutDir: true,
        rollupOptions: {
            output: {
                entryFileNames: "viewer.js",
                assetFileNames: asset => asset.name?.endsWith(".css") ? "viewer.css" : "[name][extname]"
            }
        }
    },
    plugins: [
        {
            name: "copy-ifc-runtime",
            closeBundle() {
                const output = resolve(import.meta.dirname, "../Web/dist");
                const wasmOutput = resolve(output, "wasm");
                mkdirSync(wasmOutput, { recursive: true });
                cpSync(
                    resolve(import.meta.dirname, "node_modules/@thatopen/fragments/dist/Worker/worker.mjs"),
                    resolve(output, "worker.mjs"));
                cpSync(
                    resolve(import.meta.dirname, "node_modules/web-ifc/web-ifc.wasm"),
                    resolve(wasmOutput, "web-ifc.wasm"));
                cpSync(
                    resolve(import.meta.dirname, "node_modules/web-ifc/web-ifc-mt.wasm"),
                    resolve(wasmOutput, "web-ifc-mt.wasm"));
            }
        }
    ]
});
