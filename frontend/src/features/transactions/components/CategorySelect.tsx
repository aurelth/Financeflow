import { useState } from "react";
import { Check, ChevronsUpDown, Search } from "lucide-react";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import CategoryIcon from "../../categories/components/CategoryIcon";
import type { Category } from "../../categories/types/category.types";

interface CategorySelectProps {
  categories: Category[];
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  nullable?: boolean;
  nullLabel?: string;
}

export default function CategorySelect({
  categories,
  value,
  onChange,
  placeholder = "Selecionar categoria",
  nullable = false,
  nullLabel = "Todas as categorias",
}: CategorySelectProps) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState("");

  const selected = categories.find((c) => c.id === value);
  const filtered = categories.filter((c) =>
    c.name.toLowerCase().includes(search.toLowerCase()),
  );

  function handleSelect(id: string) {
    onChange(id);
    setOpen(false);
    setSearch("");
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          className="w-full flex items-center justify-between gap-2 rounded-xl px-4 py-2.5 text-sm transition-colors focus:outline-none"
          style={{
            background: "var(--ff-bg-elevated)",
            border: `1px solid ${open ? "var(--ff-emerald)" : "var(--ff-border)"}`,
            color: selected ? "var(--ff-text-primary)" : "var(--ff-text-muted)",
          }}
        >
          <span className="flex items-center gap-2 min-w-0">
            {selected ? (
              <>
                <CategoryIcon
                  icon={selected.icon}
                  color={selected.color}
                  size={14}
                />
                <span className="truncate">{selected.name}</span>
              </>
            ) : (
              <span>{placeholder}</span>
            )}
          </span>
          <ChevronsUpDown
            size={14}
            style={{ color: "var(--ff-text-muted)", flexShrink: 0 }}
          />
        </button>
      </PopoverTrigger>

      <PopoverContent
        className="p-0 rounded-xl shadow-xl w-[var(--radix-popover-trigger-width)]"
        style={{
          background: "var(--ff-bg-card)",
          border: "1px solid var(--ff-border)",
        }}
        align="start"
        sideOffset={4}
      >
        <div
          className="flex items-center gap-2 px-3 py-2.5"
          style={{ borderBottom: "1px solid var(--ff-border)" }}
        >
          <Search
            size={13}
            style={{ color: "var(--ff-text-muted)", flexShrink: 0 }}
          />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Pesquisar..."
            autoFocus
            className="flex-1 bg-transparent text-sm focus:outline-none"
            style={{ color: "var(--ff-text-primary)" }}
          />
        </div>

        <div className="max-h-56 overflow-y-auto py-1">
          {nullable && (
            <button
              onClick={() => handleSelect("")}
              className="w-full flex items-center gap-2 px-3 py-2 text-sm transition-colors"
              style={{
                color: !value ? "var(--ff-emerald)" : "var(--ff-text-muted)",
                background: !value ? "var(--ff-emerald-subtle)" : "transparent",
              }}
              onMouseEnter={(e) => {
                if (value)
                  e.currentTarget.style.background = "var(--ff-bg-elevated)";
              }}
              onMouseLeave={(e) => {
                if (value) e.currentTarget.style.background = "transparent";
              }}
            >
              <Check
                size={13}
                style={{ flexShrink: 0, opacity: !value ? 1 : 0 }}
              />
              {nullLabel}
            </button>
          )}

          {filtered.length === 0 ? (
            <p
              className="text-sm text-center py-4"
              style={{ color: "var(--ff-text-muted)" }}
            >
              Nenhuma categoria encontrada.
            </p>
          ) : (
            filtered.map((c) => (
              <button
                key={c.id}
                onClick={() => handleSelect(c.id)}
                className="w-full flex items-center gap-2 px-3 py-2 text-sm transition-colors"
                style={{
                  color:
                    value === c.id
                      ? "var(--ff-emerald)"
                      : "var(--ff-text-secondary)",
                  background:
                    value === c.id ? "var(--ff-emerald-subtle)" : "transparent",
                }}
                onMouseEnter={(e) => {
                  if (value !== c.id)
                    e.currentTarget.style.background = "var(--ff-bg-elevated)";
                }}
                onMouseLeave={(e) => {
                  if (value !== c.id)
                    e.currentTarget.style.background = "transparent";
                }}
              >
                <Check
                  size={13}
                  style={{ flexShrink: 0, opacity: value === c.id ? 1 : 0 }}
                />
                {c.isGoalCategory ? (
                  <span style={{ fontSize: "14px", lineHeight: 1 }}>🎯</span>
                ) : (
                  <CategoryIcon icon={c.icon} color={c.color} size={14} />
                )}
                <span>{c.name}</span>
              </button>
            ))
          )}
        </div>
      </PopoverContent>
    </Popover>
  );
}
