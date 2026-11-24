// Espelha QuickEntryDtos.cs. Os enums chegam como string (JsonStringEnumConverter no back-end).

export interface QuickEntryDraft {
  type:         'Expense' | 'Income'
  amount:       number
  date:         string
  description:  string
  categoryId:   string | null
  categoryName: string | null
}

export interface AiQuota {
  limit:       number
  used:        number
  remaining:   number
  resetsAtUtc: string
}

export interface QuickEntryResult {
  drafts: QuickEntryDraft[]
  quota:  AiQuota
}

// Rascunhos aceitos pelo formulário, junto com o comprovante que os originou (anexado ao salvar)
export interface QuickEntryBatch {
  drafts: QuickEntryDraft[]
  image:  File | null
}
