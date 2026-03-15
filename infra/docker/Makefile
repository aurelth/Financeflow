.PHONY: up down restart logs ps clean

# Sobe todos os containers em background
up:
	docker compose up -d

# Derruba todos os containers
down:
	docker compose down

# Reinicia todos os containers
restart:
	docker compose down && docker compose up -d

# Exibe logs em tempo real (Ctrl+C para sair)
logs:
	docker compose logs -f

# Exibe logs de um serviço específico (ex: make logs-s service=kafka)
logs-s:
	docker compose logs -f $(service)

# Lista containers e status
ps:
	docker compose ps

# Remove containers, volumes e imagens (reset completo)
clean:
	docker compose down -v --rmi local