#!/bin/bash

KAFKA_CONTAINER="financeflow-kafka"
BOOTSTRAP_SERVER="localhost:9092"

echo "Aguardando Kafka estar pronto..."
sleep 5

create_topic() {
  local TOPIC=$1
  local PARTITIONS=$2
  local REPLICATION=$3

  echo "Criando tópico: $TOPIC"

  docker exec $KAFKA_CONTAINER kafka-topics \
    --bootstrap-server $BOOTSTRAP_SERVER \
    --create \
    --if-not-exists \
    --topic $TOPIC \
    --partitions $PARTITIONS \
    --replication-factor $REPLICATION

  echo "Tópico $TOPIC criado com sucesso."
}

# Criar tópicos
create_topic "finance.transactions.created" 1 1
create_topic "finance.reports.requested"    1 1
create_topic "finance.budget.alerts"        1 1

# Listar tópicos criados
echo ""
echo "Tópicos disponíveis no Kafka:"
docker exec $KAFKA_CONTAINER kafka-topics \
  --bootstrap-server $BOOTSTRAP_SERVER \
  --list